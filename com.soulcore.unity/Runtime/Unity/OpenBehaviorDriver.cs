using System;
using System.Collections.Generic;
using System.Globalization;
using SoulCore.Advanced;
using UnityEngine;

namespace SoulCore
{
    /// <summary>
    /// 将 <see cref="AdvancedOrchestrator"/> 产出的意图转为 <see cref="PerceptionContext"/>（可选写入 Signals 快照 Metadata，键名见 <see cref="OpenBehaviorPerceptionMetadataKeys"/>）。
    /// 同帧评估顺序：建议编排器 Update 早于本组件（见编排器类型说明）。
    /// </summary>
    [AddComponentMenu("魂核/开放式行为驱动器 v1")]
    [DisallowMultipleComponent]
    public class OpenBehaviorDriver : MonoBehaviour
    {
        private enum TaskNpcMatchMode
        {
            [InspectorName("仅标签")]
            TagOnly,
            [InspectorName("仅图层")]
            LayerOnly,
            [InspectorName("标签或图层")]
            TagOrLayer
        }

        public enum TaskNpcSocialPolicy
        {
            [InspectorName("无限制")]
            Unrestricted,
            [InspectorName("范围限制")]
            RangeLimited,
            [InspectorName("忙碌静默")]
            SilentWhenBusy
        }

        private enum SocializeBlockReason
        {
            None,
            GlobalLimit,
            PerNpcLimit,
            PerPairLimit
        }

        [SerializeField, InspectorName("编排器"), Tooltip("不填则自动查找场景中的 AdvancedOrchestrator。")]
        private AdvancedOrchestrator _orchestrator;
        [SerializeField, InspectorName("自动收集NPC"), Tooltip("默认驱动场景中所有 SoulNPC。")]
        private bool _autoCollectNpcs = true;
        [SerializeField, InspectorName("手动NPC列表")] private List<SoulNPC> _manualNpcs = new();
        [SerializeField, Min(0.5f), InspectorName("社交冷却秒数"), Tooltip("同一 NPC 的对话冷却秒数。")]
        private float _socializeCooldownSeconds = 12f;
        [SerializeField, Min(0.5f), InspectorName("非社交冷却秒数"), Tooltip("休息/工作/独处意图触发后的冷却时间。")]
        private float _nonSocialCooldownSeconds = 8f;
        [SerializeField, Min(0.5f), InspectorName("社交受限回退冷却(秒)"), Tooltip("当社交被限流拦截时，执行替代意图后的冷却时间。")]
        private float _blockedSocialFallbackCooldownSeconds = 7f;
        [SerializeField, Min(0.1f), InspectorName("忙碌静默冷却(秒)"), Tooltip("命中“忙碌静默”后给该 NPC 一个短冷却，避免每帧重复尝试社交导致日志与计数暴涨。建议 0.5~2 秒。")]
        private float _silentWhenBusyCooldownSeconds = 1f;
        [SerializeField, Min(0.5f), InspectorName("最大社交距离(米)"), Tooltip("互动距离阈值（米），超过则不触发。")]
        private float _maxSocializeDistance = 12f;
        [SerializeField, InspectorName("关系优先选目标"), Tooltip("开启后优先选择关系更高的目标，距离作为次要因素。")]
        private bool _preferRelationTarget = true;
        [SerializeField, Range(0f, 2f), InspectorName("关系权重"), Tooltip("目标选择时关系值权重。")]
        private float _relationWeight = 1.0f;
        [SerializeField, Range(0f, 2f), InspectorName("距离权重"), Tooltip("目标选择时距离权重（越近得分越高）。")]
        private float _distanceWeight = 0.7f;
        [SerializeField, Range(0f, 1f), InspectorName("连续同目标惩罚"), Tooltip("连续选择同一目标时的惩罚比例，减少来回刷屏。")]
        private float _sameTargetPenalty = 0.35f;
        [SerializeField, InspectorName("编排器信号联动"), Tooltip("将高级编排的意图原因与关键信号写入感知上下文元数据；并用 loneliness、fe_pressure 略调选目标、搭话索引与社交被拦后的独处回退。")]
        private bool _linkOrchestratorSignals = true;
        [SerializeField, InspectorName("人格参与社交表达"), Tooltip("开启后：人格信号将显式参与目标选择分数、对话语气与话题池偏好。")]
        private bool _personalityAffectsSocialExpression = true;
        [SerializeField, InspectorName("人格倾向预设"), Tooltip("可选：拖入 OpenBehaviorPersonalityPreset 资产，用于一键应用人格表达参数。")]
        private OpenBehaviorPersonalityPreset _personalityPreset;
        [SerializeField, InspectorName("人格影响目标选择"), Tooltip("开启后，psa/per 与社交匹配会进入目标打分。")]
        private bool _personalityAffectsTargetSelection = true;
        [SerializeField, Range(0f, 1f), InspectorName("目标选择人格权重"), Tooltip("人格对目标选择分数的影响强度。建议 0.3~0.7。")]
        private float _personalityTargetSelectionWeight = 0.5f;
        [SerializeField, InspectorName("人格影响对话语气"), Tooltip("开启后，按人格信号添加语气前缀。")]
        private bool _personalityAffectsStylePrefix = true;
        [SerializeField, Range(0f, 1f), InspectorName("语气人格权重"), Tooltip("人格对语气前缀触发阈值的影响强度。建议 0.4~0.8。")]
        private float _personalityStyleWeight = 0.6f;
        [SerializeField, InspectorName("人格影响话题偏好"), Tooltip("开启后，按人格在常规/任务/关怀话题池中做偏好选择。")]
        private bool _personalityAffectsTopicPreference = true;
        [SerializeField, Range(0f, 1f), InspectorName("话题人格权重"), Tooltip("人格对话题池选择的影响强度。建议 0.4~0.8。")]
        private float _personalityTopicWeight = 0.6f;
        [SerializeField, InspectorName("任务期间人格权重覆盖"), Tooltip("开启后：当 NPC 命中任务判定时，对目标选择/语气/话题的人格权重应用乘子。用于任务期临时调弱或调强人格影响。")]
        private bool _taskPeriodPersonalityWeightOverride;
        [SerializeField, Range(0f, 2f), InspectorName("任务期目标选择权重乘子"), Tooltip("仅在开启“任务期间人格权重覆盖”时生效。建议 0.4~1.2。")]
        private float _taskPeriodTargetSelectionWeightMultiplier = 0.7f;
        [SerializeField, Range(0f, 2f), InspectorName("任务期语气权重乘子"), Tooltip("仅在开启“任务期间人格权重覆盖”时生效。建议 0.4~1.2。")]
        private float _taskPeriodStyleWeightMultiplier = 0.8f;
        [SerializeField, Range(0f, 2f), InspectorName("任务期话题权重乘子"), Tooltip("仅在开启“任务期间人格权重覆盖”时生效。建议 0.4~1.2。")]
        private float _taskPeriodTopicWeightMultiplier = 0.8f;
        [SerializeField, InspectorName("紧急任务额外人格权重覆盖"), Tooltip("开启后：当 NPC 命中紧急任务判定时，在任务期乘子基础上再乘一层“紧急任务额外乘子”。")]
        private bool _urgentTaskExtraPersonalityWeightOverride = true;
        [SerializeField, Range(0f, 2f), InspectorName("紧急任务目标选择额外乘子"), Tooltip("仅在开启“紧急任务额外人格权重覆盖”时生效。建议 0.3~1.0。")]
        private float _urgentTaskTargetSelectionWeightMultiplier = 0.7f;
        [SerializeField, Range(0f, 2f), InspectorName("紧急任务语气额外乘子"), Tooltip("仅在开启“紧急任务额外人格权重覆盖”时生效。建议 0.3~1.0。")]
        private float _urgentTaskStyleWeightMultiplier = 0.7f;
        [SerializeField, Range(0f, 2f), InspectorName("紧急任务话题额外乘子"), Tooltip("仅在开启“紧急任务额外人格权重覆盖”时生效。建议 0.3~1.0。")]
        private float _urgentTaskTopicWeightMultiplier = 0.7f;
        [SerializeField, InspectorName("启用区域行为配置"), Tooltip("开启后按 NPC 所在区域体积（OpenBehaviorZoneVolume）应用区域行为配置（OpenBehaviorZoneProfile）。")]
        private bool _enableZoneProfiles;
        [SerializeField, Min(0.2f), InspectorName("区域扫描间隔(秒)"), Tooltip("扫描场景区域体积的间隔。开放世界建议 1~3 秒。")]
        private float _zoneScanIntervalSeconds = 1.5f;
        [SerializeField, InspectorName("区域切换日志"), Tooltip("开启后当 NPC 进入/离开区域时输出日志。")]
        private bool _zoneSwitchLog;
        [SerializeField, InspectorName("任务NPC社交策略"), Tooltip("控制“挂任务”NPC的主动社交行为：\n- 无限制：按常规逻辑。\n- 范围限制：仅在任务社交半径内找目标。\n- 忙碌静默：若命中“紧急任务标签”，期间不主动社交。")]
        private TaskNpcSocialPolicy _taskNpcSocialPolicy = TaskNpcSocialPolicy.Unrestricted;
        [SerializeField, InspectorName("紧急任务强制静默优先"), Tooltip("默认开启：命中紧急任务判定时，强制按“忙碌静默”处理，并优先于区域策略覆盖。用于防止关键任务NPC被区域配置放开后跑去社交。")]
        private bool _urgentTaskForceSilentPriority = true;
        [SerializeField, Min(0.5f), InspectorName("任务NPC社交半径(米)"), Tooltip("仅“范围限制”策略生效；建议 2~6 米。")]
        private float _taskNpcSocializeRangeMeters = 4f;
        [SerializeField, InspectorName("任务判定方式"), Tooltip("用于识别“任务NPC”的规则：仅标签 / 仅图层 / 标签或图层。")]
        private TaskNpcMatchMode _taskNpcMatchMode = TaskNpcMatchMode.TagOnly;
        [SerializeField, InspectorName("任务NPC标签"), Tooltip("命中这些标签的 NPC 视为“挂任务”。留空则不生效。")]
        private List<string> _taskNpcTags = new() { "QuestNpc" };
        [SerializeField, InspectorName("任务NPC图层"), Tooltip("命中该图层掩码的 NPC 视为“挂任务”（按“任务判定方式”生效）。")]
        private LayerMask _taskNpcLayerMask;
        [SerializeField, InspectorName("任务NPC直指定"), Tooltip("直接拖入任务NPC对象（SoulNPC）。命中后视为任务NPC，不依赖标签/图层。")]
        private List<SoulNPC> _taskNpcDirectList = new();
        [SerializeField, InspectorName("紧急任务标签"), Tooltip("仅“忙碌静默”策略生效；命中这些标签的任务NPC会暂停主动社交。")]
        private List<string> _urgentTaskTags = new() { "QuestNpcUrgent" };
        [SerializeField, InspectorName("紧急任务NPC直指定"), Tooltip("直接拖入紧急任务NPC对象（SoulNPC）。命中后在“忙碌静默”下暂停主动社交。")]
        private List<SoulNPC> _urgentTaskNpcDirectList = new();
        [SerializeField, InspectorName("启用自定义任务判定"), Tooltip("开启后可挂一个实现 IOpenBehaviorTaskConstraintProvider 的组件，自定义判定任务/紧急任务。")]
        private bool _useCustomTaskConstraintProvider;
        [SerializeField, InspectorName("自定义任务判定组件"), Tooltip("需实现 IOpenBehaviorTaskConstraintProvider；为空时退回内置标签/图层判定。")]
        private MonoBehaviour _taskConstraintProviderBehaviour;
        [SerializeField, InspectorName("调试日志"), Tooltip("行为驱动日志。")]
        private bool _verboseLog;
        [SerializeField, Min(0.5f), InspectorName("调试输出间隔(秒)"), Tooltip("在未触发社交时，按该间隔输出一次意图分值。")]
        private float _debugLogIntervalSeconds = 3f;
        [SerializeField, InspectorName("驱动诊断日志"), Tooltip("开启后按间隔输出每个 NPC 的 social/rest/work/solitude 四值。")]
        private bool _driveDiagnosticsLog;
        [SerializeField, InspectorName("诊断附带人格信号"), Tooltip("仅在开启「驱动诊断日志」时生效；将 personality_social_approach / personality_task_focus / personality_emotional_reactivity 一并打印。默认关闭，排障时可开。")]
        private bool _driveDiagnosticsWithPersonalitySignals;
        [SerializeField, Min(0.5f), InspectorName("驱动诊断间隔(秒)"), Tooltip("建议 2~5 秒，过小会刷屏。")]
        private float _driveDiagnosticsIntervalSeconds = 3f;
        [SerializeField, InspectorName("任务约束诊断日志"), Tooltip("默认关闭；开启后按间隔打印任务判定状态：isTaskNpc / isUrgentTaskNpc / policy / willSilenceSocialize。用于排查“忙碌静默”未命中的原因。")]
        private bool _taskConstraintDiagnosticsLog;
        [SerializeField, Min(0.5f), InspectorName("任务约束诊断间隔(秒)"), Tooltip("仅在开启「任务约束诊断日志」时生效；建议 2~5 秒。")]
        private float _taskConstraintDiagnosticsIntervalSeconds = 3f;
        [SerializeField, InspectorName("诊断附带人格生效权重"), Tooltip("仅在开启「任务约束诊断日志」时生效；附带打印 target/style/topic 三项当前生效的人格权重（含任务期与紧急任务覆盖）。")]
        private bool _taskConstraintDiagnosticsWithPersonalityWeights;
        [SerializeField, Min(0f), InspectorName("启动静默秒数"), Tooltip("启动后这段时间内，不输出“未获取到意图”日志，避免初始化噪声。")]
        private float _startupSilentSeconds = 2.5f;
        [SerializeField, Min(1), InspectorName("每分钟最大社交次数"), Tooltip("全局硬限流：1 分钟内最多触发的自动社交次数。\n建议：日常体验约 32～48；约 10 个 NPC 的活跃压测可到 52～64。")]
        private int _maxSocializeEventsPerMinute = 36;
        [SerializeField, Min(1), InspectorName("每个NPC每分钟上限"), Tooltip("单个 NPC 在 1 分钟内最多主动触发社交次数。\n建议：常规 2～4。≥5 易被感知为「话痨」；压测时可临时调高。")]
        private int _maxSocializePerNpcPerMinute = 4;
        [SerializeField, Min(1), InspectorName("每对NPC每分钟上限"), Tooltip("同一对 NPC 在 1 分钟内最多触发社交次数。\n建议：常规 1～2。≥3 易出现固定搭档「二人转」复读；压测可到 4～6。")]
        private int _maxSocializePerPairPerMinute = 3;

        private readonly Dictionary<SoulNPC, float> _cooldowns = new();
        private readonly Dictionary<SoulNPC, SoulNPC> _lastTargets = new();
        private readonly Dictionary<SoulNPC, float> _nextIntentLogAt = new();
        private readonly Dictionary<SoulNPC, float> _nextDriveDiagLogAt = new();
        private readonly Dictionary<SoulNPC, float> _nextInitMissLogAt = new();
        private readonly Dictionary<SoulNPC, float> _nextPerNpcLimitLogAt = new();
        private readonly Dictionary<SoulNPC, float> _nextGlobalLimitLogAt = new();
        private readonly Dictionary<SoulNPC, float> _nextTaskPolicyLogAt = new();
        private readonly Dictionary<SoulNPC, float> _nextTaskStatusLogAt = new();
        private readonly Dictionary<SoulNPC, int> _urgentSilentHitCount = new();
        private readonly Dictionary<int, Queue<float>> _urgentSilentHitTimestamps = new();
        private readonly Dictionary<SoulNPC, AdvancedIntentKind> _lastIntentKinds = new();
        private readonly Dictionary<SoulNPC, float> _lastIntentScores = new();
        private readonly Queue<float> _socializeTimestamps = new();
        private readonly Dictionary<int, Queue<float>> _perNpcSocializeTimestamps = new();
        private readonly Dictionary<ulong, Queue<float>> _perPairSocializeTimestamps = new();
        private bool _loggedStartup;
        private float _startedAt;
        private int _blockedByGlobalLimit;
        private int _blockedByPerNpcLimit;
        private int _blockedByPerPairLimit;
        private int _socializeSuccessCount;
        private bool _providerTypeWarned;
        private float _nextTaskPolicyConfigWarnAt;
        private OpenBehaviorZoneVolume[] _zoneVolumes = Array.Empty<OpenBehaviorZoneVolume>();
        private float _nextZoneScanAt;
        private readonly Dictionary<SoulNPC, OpenBehaviorZoneProfile> _lastZoneProfileByNpc = new();

        public int BlockedByGlobalLimit => _blockedByGlobalLimit;
        public int BlockedByPerNpcLimit => _blockedByPerNpcLimit;
        public int BlockedByPerPairLimit => _blockedByPerPairLimit;
        public int SocializeSuccessCount => _socializeSuccessCount;

        private static readonly string[] TopicPool =
        {
            "今天天气不错，你那边还顺利吗？",
            "我刚想到一个点子，想听听你的看法。",
            "最近在忙什么？要不要一起处理下手头任务？",
            "刚刚有点发呆，突然想和你聊两句。"
        };
        private static readonly string[] TopicPoolTask =
        {
            "我在梳理任务节奏，你那边有没有更高效的做法？",
            "你手头这个进度看起来不错，方便交流下关键步骤吗？",
            "我想把事情推进得更稳一点，想听听你的建议。"
        };
        private static readonly string[] TopicPoolCare =
        {
            "我想确认下你现在状态还好吗？",
            "这会儿节奏有点快，我们要不要先对齐一下感受？",
            "如果你愿意，我想先听听你现在最在意什么。"
        };
        private static readonly string[] RestPool =
        {
            "我先缓一缓，整理下状态。",
            "我想先休息一会儿，等下再继续。",
            "我先把节奏放慢一点。"
        };
        private static readonly string[] WorkPool =
        {
            "我先把手头任务处理完。",
            "我先聚焦当前工作，稍后再聊。",
            "我先推进一下当前事项。"
        };
        private static readonly string[] SolitudePool =
        {
            "我想先安静一下，等会儿再交流。",
            "我先独处一小会儿，整理下思路。",
            "我现在更想一个人待会儿。"
        };

        private void Update()
        {
            var orchestrator = _orchestrator != null ? _orchestrator : AdvancedOrchestrator.Instance;
            if (orchestrator == null)
            {
                if (_verboseLog) Debug.LogWarning("[魂核/OpenBehavior] 未找到 AdvancedOrchestrator。", this);
                return;
            }

            var npcs = _collectNpcs();
            if (_verboseLog && !_loggedStartup)
            {
                Debug.Log($"[魂核/OpenBehavior] 启动成功，收集到 NPC 数量: {npcs.Count}", this);
                _loggedStartup = true;
                _startedAt = Time.unscaledTime;
            }
            _warnTaskPolicyConfigOccasionally(npcs);
            for (var i = 0; i < npcs.Count; i++)
            {
                var source = npcs[i];
                if (source == null || source.Soul == null) continue;
                if (_isCoolingDown(source)) continue;
                if (!orchestrator.TryGetIntent(source, out var intent))
                {
                    _logInitMissOccasionally(source);
                    continue;
                }
                _logZoneSwitchIfChanged(source);
                _logDriveDiagnosticsOccasionally(orchestrator, source, intent);
                _logTaskConstraintStatusOccasionally(source);
                if (intent.Kind != AdvancedIntentKind.Socialize)
                {
                    if (intent.Kind != AdvancedIntentKind.None)
                    {
                        _executeNonSocialIntent(orchestrator, source, intent);
                    }
                    _logIntentOccasionally(source, intent);
                    continue;
                }
                var effectiveTaskPolicy = _effectiveTaskNpcSocialPolicy(source);
            var isUrgentTaskNpc = _isUrgentTaskNpc(source);
            if (effectiveTaskPolicy == TaskNpcSocialPolicy.SilentWhenBusy && isUrgentTaskNpc)
                {
                _urgentSilentHitCount[source] = _urgentSilentHitCount.TryGetValue(source, out var hit) ? hit + 1 : 1;
                _recordUrgentSilentHit(source);
                _cooldowns[source] = Time.unscaledTime + Mathf.Max(0.1f, _silentWhenBusyCooldownSeconds);
                    _logTaskPolicyOccasionally(source, "忙碌静默：紧急任务期间暂停主动社交");
                    continue;
                }

                var socialRangeCap = _socialRangeCapForSource(source);
                var target = _pickTarget(orchestrator, source, npcs, socialRangeCap);
                if (target == null) continue;
                if (!_withinDistance(source.transform.position, target.transform.position, socialRangeCap)) continue;
                var blockReason = _canTriggerSocializeNow(source, target);
                if (blockReason != SocializeBlockReason.None)
                {
                    _countBlocked(blockReason);
                    _tryExecuteFallbackAfterSocialBlocked(orchestrator, source, target, blockReason);
                    continue;
                }

                var line = _composeLine(orchestrator, source, target, intent);
                var convIntensity = _conversationIntensity(orchestrator, source, intent);
                var ctx = new PerceptionContext("conversation", line, convIntensity, source.CharacterName);
                _enrichPerceptionFromOrchestrator(orchestrator, source, intent, ref ctx);
                target.TryEnqueuePerceive(ctx, null, false);
                _markSocializeTriggered(source, target);
                _socializeSuccessCount++;
                _cooldowns[source] = Time.unscaledTime + Mathf.Max(0.5f, _socializeCooldownSeconds);
                _lastTargets[source] = target;

                if (_verboseLog)
                {
                    Debug.Log($"[魂核/OpenBehavior] {source.CharacterName} -> {target.CharacterName}: {line}", this);
                }
            }
        }

        private List<SoulNPC> _collectNpcs()
        {
            var list = new List<SoulNPC>();
            if (_autoCollectNpcs)
            {
                var found = FindObjectsOfType<SoulNPC>();
                for (var i = 0; i < found.Length; i++)
                {
                    var n = found[i];
                    if (n == null || n.Soul == null) continue;
                    list.Add(n);
                }
            }

            for (var i = 0; i < _manualNpcs.Count; i++)
            {
                var n = _manualNpcs[i];
                if (n == null || n.Soul == null) continue;
                if (!list.Contains(n)) list.Add(n);
            }
            return list;
        }

        private bool _isCoolingDown(SoulNPC npc)
        {
            if (npc == null) return true;
            if (!_cooldowns.TryGetValue(npc, out var until)) return false;
            return Time.unscaledTime < until;
        }

        private SoulNPC _pickTarget(AdvancedOrchestrator orchestrator, SoulNPC source, List<SoulNPC> npcs, float maxDistanceForSource)
        {
            if (!_preferRelationTarget)
            {
                return _pickNearestTarget(source, npcs, maxDistanceForSource);
            }

            SoulNPC best = null;
            var bestScore = float.MinValue;
            for (var i = 0; i < npcs.Count; i++)
            {
                var n = npcs[i];
                if (n == null || n == source || n.Soul == null) continue;
                var sqrDist = (source.transform.position - n.transform.position).sqrMagnitude;
                if (maxDistanceForSource > 0f && sqrDist > maxDistanceForSource * maxDistanceForSource) continue;
                var score = _targetScore(orchestrator, source, n, sqrDist);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = n;
                }
            }
            return best;
        }

        private SoulNPC _pickNearestTarget(SoulNPC source, List<SoulNPC> npcs, float maxDistanceForSource)
        {
            SoulNPC best = null;
            var bestDist = float.MaxValue;
            for (var i = 0; i < npcs.Count; i++)
            {
                var n = npcs[i];
                if (n == null || n == source || n.Soul == null) continue;
                var d = (source.transform.position - n.transform.position).sqrMagnitude;
                if (maxDistanceForSource > 0f && d > maxDistanceForSource * maxDistanceForSource) continue;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = n;
                }
            }
            return best;
        }

        private float _targetScore(AdvancedOrchestrator orchestrator, SoulNPC source, SoulNPC target, float sqrDist)
        {
            var relationNorm = _normalizedRelation(source, target);
            var distNorm = 1f - Mathf.Clamp01(Mathf.Sqrt(sqrDist) / Mathf.Max(0.1f, _maxSocializeDistance));
            var score = relationNorm * _relationWeight + distNorm * _distanceWeight;
            if (_lastTargets.TryGetValue(source, out var last) && last == target)
            {
                score -= Mathf.Clamp01(_sameTargetPenalty);
            }

            if (_linkOrchestratorSignals && orchestrator != null &&
                orchestrator.TryGetSignal(source, AdvancedSignalId.Loneliness, out var loneliness))
            {
                score += loneliness * 0.11f * Mathf.Lerp(0.65f, 1f, relationNorm);
            }

            if (_personalityAffectsSocialExpression && _linkOrchestratorSignals && orchestrator != null &&
                _tryGetPersonalitySignals(orchestrator, source, out var psa, out _, out var per))
            {
                var w = _effectivePersonalityTargetSelectionWeight(source);
                // 社交趋近高：更偏好高关系对象；情绪反应高：降低低关系目标的吸引力。
                if (_personalityAffectsTargetSelection)
                {
                    score += (psa - 0.5f) * 0.18f * relationNorm * w;
                    score -= per * 0.10f * (1f - relationNorm) * w;
                    if (_tryGetPersonalitySignals(orchestrator, target, out var targetPsa, out _, out _))
                    {
                        // 轻度“社交匹配”：趋近风格相近更容易被选中。
                        score += (1f - Mathf.Abs(psa - targetPsa)) * 0.05f * w;
                    }
                }
            }

            return score;
        }

        private float _conversationIntensity(AdvancedOrchestrator orchestrator, SoulNPC source, AdvancedIntent intent)
        {
            var s = Mathf.Clamp(intent.Score, 0.4f, 0.9f);
            if (!_linkOrchestratorSignals || orchestrator == null || source == null) return s;
            if (orchestrator.TryGetSignal(source, AdvancedSignalId.FePressure, out var fe))
            {
                s = Mathf.Clamp(s + fe * 0.06f, 0.4f, 0.92f);
            }

            return s;
        }

        private void _enrichPerceptionFromOrchestrator(
            AdvancedOrchestrator orchestrator,
            SoulNPC source,
            AdvancedIntent intent,
            ref PerceptionContext ctx)
        {
            if (!_linkOrchestratorSignals || orchestrator == null || source == null) return;
            ctx.Metadata[OpenBehaviorPerceptionMetadataKeys.AdvancedIntentKind] = intent.Kind.ToString();
            if (!string.IsNullOrEmpty(intent.Reason))
            {
                var r = intent.Reason;
                if (r.Length > 160) r = r.Substring(0, 160);
                ctx.Metadata[OpenBehaviorPerceptionMetadataKeys.AdvancedIntentReason] = r;
            }

            _putSignalMeta(ctx, orchestrator, source, AdvancedSignalId.Loneliness);
            _putSignalMeta(ctx, orchestrator, source, AdvancedSignalId.FePressure);
            _putSignalMeta(ctx, orchestrator, source, AdvancedSignalId.ConsciousnessLoad);
            _putSignalMeta(ctx, orchestrator, source, AdvancedSignalId.NarrativeTension);
            _putSignalMeta(ctx, orchestrator, source, AdvancedSignalId.PersonalitySocialApproach);
            _putSignalMeta(ctx, orchestrator, source, AdvancedSignalId.PersonalityTaskFocus);
            _putSignalMeta(ctx, orchestrator, source, AdvancedSignalId.PersonalityEmotionalReactivity);
        }

        private static void _putSignalMeta(PerceptionContext ctx, AdvancedOrchestrator orchestrator, SoulNPC source, AdvancedSignalId signalId)
        {
            if (!orchestrator.TryGetSignal(source, signalId, out var v)) return;
            ctx.Metadata[OpenBehaviorPerceptionMetadataKeys.SignalSnapshot(signalId)] =
                v.ToString("F3", CultureInfo.InvariantCulture);
        }

        private static float _normalizedRelation(SoulNPC source, SoulNPC target)
        {
            if (source == null || source.Soul == null || target == null || target.Soul == null) return 0.5f;
            var raw = source.Soul.GetRelation(target.Soul.Id); // -10..10
            return Mathf.InverseLerp(-10f, 10f, raw);
        }

        private static bool _withinDistance(Vector3 a, Vector3 b, float maxDistance)
        {
            if (maxDistance <= 0f) return true;
            return Vector3.SqrMagnitude(a - b) <= maxDistance * maxDistance;
        }

        private float _socialRangeCapForSource(SoulNPC source)
        {
            var baseCap = Mathf.Max(0f, _maxSocializeDistance);
            if (_effectiveTaskNpcSocialPolicy(source) != TaskNpcSocialPolicy.RangeLimited) return baseCap;
            if (!_isTaskNpc(source)) return baseCap;
            var taskCap = _effectiveTaskNpcSocializeRangeMeters(source);
            if (baseCap <= 0f) return taskCap;
            return Mathf.Min(baseCap, taskCap);
        }

        private bool _isTaskNpc(SoulNPC npc)
        {
            if (npc == null) return false;
            if (_tryCustomConstraintProvider(out var p))
            {
                return p.IsTaskNpc(npc);
            }

            var byTag = _matchTags(npc, _taskNpcTags);
            var byLayer = _matchLayer(npc, _taskNpcLayerMask);
            var byUrgentTag = _matchTags(npc, _urgentTaskTags);
            var byDirectTaskNpc = _matchDirectNpc(npc, _taskNpcDirectList);
            var byDirectUrgentNpc = _matchDirectNpc(npc, _urgentTaskNpcDirectList);
            return _taskNpcMatchMode switch
            {
                TaskNpcMatchMode.TagOnly => byTag || byDirectTaskNpc,
                TaskNpcMatchMode.LayerOnly => byLayer || byDirectTaskNpc,
                TaskNpcMatchMode.TagOrLayer => byTag || byLayer || byDirectTaskNpc,
                _ => byTag || byDirectTaskNpc
            } || byUrgentTag || byDirectUrgentNpc; // 防踩坑：命中紧急标签/直指定紧急NPC时，自动视为任务NPC
        }

        private bool _isUrgentTaskNpc(SoulNPC npc)
        {
            if (npc == null || !_isTaskNpc(npc)) return false;
            if (_tryCustomConstraintProvider(out var p))
            {
                return p.IsUrgentTaskNpc(npc);
            }
            return _matchTags(npc, _urgentTaskTags) || _matchDirectNpc(npc, _urgentTaskNpcDirectList);
        }

        private bool _tryCustomConstraintProvider(out IOpenBehaviorTaskConstraintProvider provider)
        {
            provider = null;
            if (!_useCustomTaskConstraintProvider || _taskConstraintProviderBehaviour == null) return false;
            provider = _taskConstraintProviderBehaviour as IOpenBehaviorTaskConstraintProvider;
            if (provider == null && !_providerTypeWarned)
            {
                _providerTypeWarned = true;
                Debug.LogWarning(
                    "[魂核/OpenBehavior] 已启用「自定义任务判定」，但指定组件未实现 IOpenBehaviorTaskConstraintProvider，已自动回退内置标签/图层判定。",
                    this);
            }
            return provider != null;
        }

        private static bool _matchTags(SoulNPC npc, List<string> tags)
        {
            if (npc == null || tags == null || tags.Count == 0) return false;
            var tag = npc.tag;
            if (string.IsNullOrEmpty(tag)) return false;
            for (var i = 0; i < tags.Count; i++)
            {
                var t = tags[i];
                if (string.IsNullOrWhiteSpace(t)) continue;
                if (string.Equals(tag, t.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static bool _matchLayer(SoulNPC npc, LayerMask mask)
        {
            if (npc == null || npc.gameObject == null) return false;
            var bit = 1 << npc.gameObject.layer;
            return (mask.value & bit) != 0;
        }

        private static bool _matchDirectNpc(SoulNPC npc, List<SoulNPC> list)
        {
            if (npc == null || list == null || list.Count == 0) return false;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == npc) return true;
            }
            return false;
        }

        private void _logTaskPolicyOccasionally(SoulNPC source, string message)
        {
            if (!_verboseLog || source == null) return;
            var now = Time.unscaledTime;
            if (_nextTaskPolicyLogAt.TryGetValue(source, out var nextAt) && now < nextAt) return;
            _nextTaskPolicyLogAt[source] = now + 8f;
            Debug.Log($"[魂核/OpenBehavior] {source.CharacterName} {message}", this);
        }

        private void _logTaskConstraintStatusOccasionally(SoulNPC source)
        {
            if (!_taskConstraintDiagnosticsLog || source == null) return;
            var now = Time.unscaledTime;
            if (_nextTaskStatusLogAt.TryGetValue(source, out var nextAt) && now < nextAt) return;
            _nextTaskStatusLogAt[source] = now + Mathf.Max(0.5f, _taskConstraintDiagnosticsIntervalSeconds);

            var isTaskNpc = _isTaskNpc(source);
            var isUrgentTaskNpc = _isUrgentTaskNpc(source);
            var effectivePolicy = _effectiveTaskNpcSocialPolicy(source);
            var willSilenceSocialize = effectivePolicy == TaskNpcSocialPolicy.SilentWhenBusy && isUrgentTaskNpc;
            var forcedByUrgentPriority = _urgentTaskForceSilentPriority && isUrgentTaskNpc;
            var urgentSilentHits = _urgentSilentHitCount.TryGetValue(source, out var hitCount) ? hitCount : 0;
            var urgentSilentHitsPerMinute = _urgentSilentHitsLastMinute(source);
            var policyName = _taskNpcSocialPolicy switch
            {
                TaskNpcSocialPolicy.Unrestricted => "无限制",
                TaskNpcSocialPolicy.RangeLimited => "范围限制",
                TaskNpcSocialPolicy.SilentWhenBusy => "忙碌静默",
                _ => _taskNpcSocialPolicy.ToString()
            };
            var effectivePolicyName = effectivePolicy switch
            {
                TaskNpcSocialPolicy.Unrestricted => "无限制",
                TaskNpcSocialPolicy.RangeLimited => "范围限制",
                TaskNpcSocialPolicy.SilentWhenBusy => "忙碌静默",
                _ => effectivePolicy.ToString()
            };
            var zoneName = _effectiveZoneProfile(source)?.profileName;
            Debug.Log(
                $"[魂核/OpenBehavior][任务约束诊断] {source.CharacterName} " +
                $"isTaskNpc={isTaskNpc} isUrgentTaskNpc={isUrgentTaskNpc} " +
                $"policy={policyName} effectivePolicy={effectivePolicyName} " +
                $"zone={(string.IsNullOrWhiteSpace(zoneName) ? "none" : zoneName)} " +
                $"willSilenceSocialize={willSilenceSocialize} urgentForceSilent={forcedByUrgentPriority} " +
                $"urgentSilentHits={urgentSilentHits} urgentSilentPerMin={urgentSilentHitsPerMinute}{_buildPersonalityWeightDiagSuffix(source)}",
                this);
        }

        private string _buildPersonalityWeightDiagSuffix(SoulNPC source)
        {
            if (!_taskConstraintDiagnosticsWithPersonalityWeights || source == null) return string.Empty;
            var targetW = _effectivePersonalityTargetSelectionWeight(source);
            var styleW = _effectivePersonalityStyleWeight(source);
            var topicW = _effectivePersonalityTopicWeight(source);
            return
                $" | effW(target={targetW.ToString("F2", CultureInfo.InvariantCulture)}" +
                $" style={styleW.ToString("F2", CultureInfo.InvariantCulture)}" +
                $" topic={topicW.ToString("F2", CultureInfo.InvariantCulture)})";
        }

        private void _warnTaskPolicyConfigOccasionally(List<SoulNPC> npcs)
        {
            if (npcs == null) return;
            if (_taskNpcSocialPolicy != TaskNpcSocialPolicy.RangeLimited &&
                _taskNpcSocialPolicy != TaskNpcSocialPolicy.SilentWhenBusy)
            {
                return;
            }

            var now = Time.unscaledTime;
            if (now < _nextTaskPolicyConfigWarnAt) return;
            _nextTaskPolicyConfigWarnAt = now + 10f;

            var taskCount = 0;
            var urgentCount = 0;
            for (var i = 0; i < npcs.Count; i++)
            {
                var npc = npcs[i];
                if (npc == null || npc.Soul == null) continue;
                if (_isTaskNpc(npc)) taskCount++;
                if (_isUrgentTaskNpc(npc)) urgentCount++;
            }

            if (_taskNpcSocialPolicy == TaskNpcSocialPolicy.RangeLimited && taskCount <= 0)
            {
                Debug.LogWarning(
                    "[魂核/OpenBehavior] 当前策略为「范围限制」，但场景中未命中任何任务NPC（taskCount=0）。请检查任务标签/图层/任务NPC直指定或自定义判定组件配置。",
                    this);
                return;
            }

            if (_taskNpcSocialPolicy == TaskNpcSocialPolicy.SilentWhenBusy)
            {
                if (taskCount <= 0)
                {
                    Debug.LogWarning(
                        "[魂核/OpenBehavior] 当前策略为「忙碌静默」，但场景中未命中任何任务NPC（taskCount=0）。静默不会生效，请检查任务标签/图层/任务NPC直指定或自定义判定组件配置。",
                        this);
                }
                else if (urgentCount <= 0)
                {
                    Debug.LogWarning(
                        "[魂核/OpenBehavior] 当前策略为「忙碌静默」，已命中任务NPC但未命中紧急任务NPC（urgentCount=0）。主动社交不会被静默，请检查紧急任务标签/紧急任务NPC直指定或自定义判定。",
                        this);
                }
            }
        }

        private SocializeBlockReason _canTriggerSocializeNow(SoulNPC source, SoulNPC target)
        {
            var now = Time.unscaledTime;
            _trimWindow(_socializeTimestamps, now);
            if (_socializeTimestamps.Count >= Mathf.Max(1, _maxSocializeEventsPerMinute)) return SocializeBlockReason.GlobalLimit;

            if (source != null)
            {
                var npcQueue = _getNpcQueue(source.GetInstanceID());
                _trimWindow(npcQueue, now);
                if (npcQueue.Count >= Mathf.Max(1, _maxSocializePerNpcPerMinute)) return SocializeBlockReason.PerNpcLimit;
            }

            if (source != null && target != null)
            {
                var pairQueue = _getPairQueue(source.GetInstanceID(), target.GetInstanceID());
                _trimWindow(pairQueue, now);
                if (pairQueue.Count >= Mathf.Max(1, _maxSocializePerPairPerMinute)) return SocializeBlockReason.PerPairLimit;
            }
            return SocializeBlockReason.None;
        }

        private void _markSocializeTriggered(SoulNPC source, SoulNPC target)
        {
            var now = Time.unscaledTime;
            _socializeTimestamps.Enqueue(now);
            if (source != null)
            {
                _getNpcQueue(source.GetInstanceID()).Enqueue(now);
            }
            if (source != null && target != null)
            {
                _getPairQueue(source.GetInstanceID(), target.GetInstanceID()).Enqueue(now);
            }
        }

        private void _logIntentOccasionally(SoulNPC source, AdvancedIntent intent)
        {
            if (!_verboseLog || source == null) return;
            var now = Time.unscaledTime;
            if (_nextIntentLogAt.TryGetValue(source, out var nextAt) && now < nextAt) return;

            // 避免重复输出同一意图同一分值：只有意图类型变化或分值变化明显才输出。
            var scoreChangedEnough = true;
            if (_lastIntentScores.TryGetValue(source, out var lastScore))
            {
                scoreChangedEnough = Mathf.Abs(lastScore - intent.Score) >= 0.02f;
            }
            var kindChanged = !_lastIntentKinds.TryGetValue(source, out var lastKind) || lastKind != intent.Kind;
            if (!kindChanged && !scoreChangedEnough) return;

            _nextIntentLogAt[source] = now + Mathf.Max(0.5f, _debugLogIntervalSeconds);
            _lastIntentKinds[source] = intent.Kind;
            _lastIntentScores[source] = intent.Score;
            Debug.Log($"[魂核/OpenBehavior] {source.CharacterName} 意图={intent.Kind} score={intent.Score:F2}", this);
        }

        private void _logInitMissOccasionally(SoulNPC source)
        {
            if (!_verboseLog || source == null) return;
            var now = Time.unscaledTime;
            if (now - _startedAt < Mathf.Max(0f, _startupSilentSeconds)) return;
            if (_nextInitMissLogAt.TryGetValue(source, out var nextAt) && now < nextAt) return;
            _nextInitMissLogAt[source] = now + 8f;
            Debug.Log($"[魂核/OpenBehavior] {source.name} 未获取到意图（可能尚未初始化）", this);
        }

        private void _logDriveDiagnosticsOccasionally(AdvancedOrchestrator orchestrator, SoulNPC source, AdvancedIntent intent)
        {
            if (!_verboseLog || !_driveDiagnosticsLog || source == null || orchestrator == null) return;
            var now = Time.unscaledTime;
            if (_nextDriveDiagLogAt.TryGetValue(source, out var nextAt) && now < nextAt) return;
            _nextDriveDiagLogAt[source] = now + Mathf.Max(0.5f, _driveDiagnosticsIntervalSeconds);
            if (!orchestrator.TryGetDriveValues(source, out var social, out var rest, out var work, out var solitude)) return;
            var personalityPart = _buildPersonalitySignalDiagSuffix(orchestrator, source);
            Debug.Log(
                $"[魂核/OpenBehavior][驱动诊断] {source.CharacterName} " +
                $"social={social:F2} rest={rest:F2} work={work:F2} solitude={solitude:F2} " +
                $"-> intent={intent.Kind}({intent.Score:F2}){personalityPart}",
                this);
        }

        private string _buildPersonalitySignalDiagSuffix(AdvancedOrchestrator orchestrator, SoulNPC source)
        {
            if (!_driveDiagnosticsWithPersonalitySignals || orchestrator == null || source == null)
            {
                return string.Empty;
            }

            var hasSocial = orchestrator.TryGetSignal(source, AdvancedSignalId.PersonalitySocialApproach, out var socialApproach);
            var hasTask = orchestrator.TryGetSignal(source, AdvancedSignalId.PersonalityTaskFocus, out var taskFocus);
            var hasReactivity = orchestrator.TryGetSignal(source, AdvancedSignalId.PersonalityEmotionalReactivity, out var emotionalReactivity);
            if (!hasSocial && !hasTask && !hasReactivity)
            {
                return " | personality=missing";
            }

            return
                $" | psa={(hasSocial ? socialApproach.ToString("F2", CultureInfo.InvariantCulture) : "na")}" +
                $" ptf={(hasTask ? taskFocus.ToString("F2", CultureInfo.InvariantCulture) : "na")}" +
                $" per={(hasReactivity ? emotionalReactivity.ToString("F2", CultureInfo.InvariantCulture) : "na")}";
        }

        private string _composeLine(AdvancedOrchestrator orchestrator, SoulNPC source, SoulNPC target, AdvancedIntent intent)
        {
            var pool = TopicPool;
            string stylePrefix = string.Empty;
            if (_personalityAffectsSocialExpression && _linkOrchestratorSignals && orchestrator != null && source != null &&
                _tryGetPersonalitySignals(orchestrator, source, out var psa, out var ptf, out var per))
            {
                if (_personalityAffectsTopicPreference)
                {
                    pool = _pickTopicPoolByPersonality(psa, ptf, per, _effectivePersonalityTopicWeight(source));
                }
                if (_personalityAffectsStylePrefix)
                {
                    stylePrefix = _stylePrefixByPersonality(psa, ptf, per, _effectivePersonalityStyleWeight(source));
                }
            }

            var idx = Mathf.Abs(source.GetInstanceID() + target.GetInstanceID() + DateTime.Now.Second) % pool.Length;
            if (_linkOrchestratorSignals && orchestrator != null && source != null &&
                orchestrator.TryGetSignal(source, AdvancedSignalId.FePressure, out var fe))
            {
                idx = (idx + Mathf.FloorToInt(fe * pool.Length)) % pool.Length;
            }

            var prefix = intent.Score >= 0.75f ? "我这会儿挺想聊聊，" : "路过想问问，";
            return prefix + stylePrefix + pool[idx];
        }

        private static bool _tryGetPersonalitySignals(
            AdvancedOrchestrator orchestrator,
            SoulNPC npc,
            out float psa,
            out float ptf,
            out float per)
        {
            psa = 0.5f;
            ptf = 0.5f;
            per = 0.25f;
            if (orchestrator == null || npc == null) return false;
            var ok1 = orchestrator.TryGetSignal(npc, AdvancedSignalId.PersonalitySocialApproach, out psa);
            var ok2 = orchestrator.TryGetSignal(npc, AdvancedSignalId.PersonalityTaskFocus, out ptf);
            var ok3 = orchestrator.TryGetSignal(npc, AdvancedSignalId.PersonalityEmotionalReactivity, out per);
            return ok1 || ok2 || ok3;
        }

        private static string[] _pickTopicPoolByPersonality(float psa, float ptf, float per, float weight)
        {
            // 明确话题偏好：任务专注高 -> 任务池；情绪反应高 -> 关怀池；其余 -> 常规池。
            var taskGate = Mathf.Lerp(0.60f, 0.52f, weight);
            var careGate = Mathf.Lerp(0.30f, 0.24f, weight);
            if (ptf >= psa && ptf >= per && ptf >= taskGate) return TopicPoolTask;
            if (per > psa && per >= careGate) return TopicPoolCare;
            return TopicPool;
        }

        private static string _stylePrefixByPersonality(float psa, float ptf, float per, float weight)
        {
            var taskGate = Mathf.Lerp(0.66f, 0.58f, weight);
            var reactiveGate = Mathf.Lerp(0.34f, 0.28f, weight);
            var approachGate = Mathf.Lerp(0.66f, 0.58f, weight);
            if (ptf >= psa && ptf >= per && ptf >= taskGate) return "我想把事情说清楚些，";
            if (per > psa && per >= reactiveGate) return "我这会儿有点敏感，想先确认下，";
            if (psa >= approachGate) return "我挺想和你多靠近一点，";
            return string.Empty;
        }

        private float _effectivePersonalityTargetSelectionWeight(SoulNPC source)
        {
            var w = Mathf.Clamp01(_personalityTargetSelectionWeight);
            w = _applyTaskPersonalityWeightOverrides(
                source,
                w,
                _taskPeriodTargetSelectionWeightMultiplier,
                _urgentTaskTargetSelectionWeightMultiplier);
            var zone = _effectiveZoneProfile(source);
            if (zone != null) w = Mathf.Clamp01(w * Mathf.Max(0f, zone.personalityTargetWeightScale));
            return w;
        }

        private float _effectivePersonalityStyleWeight(SoulNPC source)
        {
            var w = Mathf.Clamp01(_personalityStyleWeight);
            w = _applyTaskPersonalityWeightOverrides(
                source,
                w,
                _taskPeriodStyleWeightMultiplier,
                _urgentTaskStyleWeightMultiplier);
            var zone = _effectiveZoneProfile(source);
            if (zone != null) w = Mathf.Clamp01(w * Mathf.Max(0f, zone.personalityStyleWeightScale));
            return w;
        }

        private float _effectivePersonalityTopicWeight(SoulNPC source)
        {
            var w = Mathf.Clamp01(_personalityTopicWeight);
            w = _applyTaskPersonalityWeightOverrides(
                source,
                w,
                _taskPeriodTopicWeightMultiplier,
                _urgentTaskTopicWeightMultiplier);
            var zone = _effectiveZoneProfile(source);
            if (zone != null) w = Mathf.Clamp01(w * Mathf.Max(0f, zone.personalityTopicWeightScale));
            return w;
        }

        private TaskNpcSocialPolicy _effectiveTaskNpcSocialPolicy(SoulNPC source)
        {
            if (_urgentTaskForceSilentPriority && _isUrgentTaskNpc(source))
            {
                return TaskNpcSocialPolicy.SilentWhenBusy;
            }
            var zone = _effectiveZoneProfile(source);
            if (zone != null && zone.overrideTaskNpcSocialPolicy) return zone.taskNpcSocialPolicy;
            return _taskNpcSocialPolicy;
        }

        private float _effectiveTaskNpcSocializeRangeMeters(SoulNPC source)
        {
            var zone = _effectiveZoneProfile(source);
            if (zone != null && zone.overrideTaskNpcSocializeRangeMeters)
            {
                return Mathf.Max(0.5f, zone.taskNpcSocializeRangeMeters);
            }
            return Mathf.Max(0.5f, _taskNpcSocializeRangeMeters);
        }

        private OpenBehaviorZoneProfile _effectiveZoneProfile(SoulNPC source)
        {
            if (!_enableZoneProfiles || source == null) return null;
            _refreshZoneVolumesIfNeeded();
            if (_zoneVolumes == null || _zoneVolumes.Length == 0) return null;

            var bestPriority = int.MinValue;
            OpenBehaviorZoneProfile best = null;
            var pos = source.transform != null ? source.transform.position : Vector3.zero;
            for (var i = 0; i < _zoneVolumes.Length; i++)
            {
                var v = _zoneVolumes[i];
                if (v == null || v.Profile == null || !v.isActiveAndEnabled) continue;
                if (!v.Contains(pos)) continue;
                if (v.Priority < bestPriority) continue;
                bestPriority = v.Priority;
                best = v.Profile;
            }
            return best;
        }

        private void _refreshZoneVolumesIfNeeded()
        {
            if (!_enableZoneProfiles) return;
            var now = Time.unscaledTime;
            if (_zoneVolumes != null && now < _nextZoneScanAt) return;
            _zoneVolumes = FindObjectsOfType<OpenBehaviorZoneVolume>();
            _nextZoneScanAt = now + Mathf.Max(0.2f, _zoneScanIntervalSeconds);
        }

        private void _logZoneSwitchIfChanged(SoulNPC source)
        {
            if (!_zoneSwitchLog || source == null) return;
            var now = _effectiveZoneProfile(source);
            _lastZoneProfileByNpc.TryGetValue(source, out var last);
            if (now == last) return;
            _lastZoneProfileByNpc[source] = now;
            var nowName = now != null && !string.IsNullOrWhiteSpace(now.profileName) ? now.profileName : "none";
            var lastName = last != null && !string.IsNullOrWhiteSpace(last.profileName) ? last.profileName : "none";
            Debug.Log($"[魂核/OpenBehavior] {source.CharacterName} 区域配置切换: {lastName} -> {nowName}", this);
        }

        private float _applyTaskPersonalityWeightOverrides(
            SoulNPC source,
            float baseWeight,
            float taskMultiplier,
            float urgentExtraMultiplier)
        {
            var w = Mathf.Clamp01(baseWeight);
            if (!_taskPeriodPersonalityWeightOverride || !_isTaskNpc(source)) return w;
            w = Mathf.Clamp01(w * Mathf.Max(0f, taskMultiplier));
            if (_urgentTaskExtraPersonalityWeightOverride && _isUrgentTaskNpc(source))
            {
                w = Mathf.Clamp01(w * Mathf.Max(0f, urgentExtraMultiplier));
            }
            return w;
        }

        private void _executeNonSocialIntent(AdvancedOrchestrator orchestrator, SoulNPC source, AdvancedIntent intent)
        {
            if (source == null || source.Soul == null) return;
            var line = _composeNonSocialLine(source, intent);
            if (string.IsNullOrEmpty(line)) return;
            var eventType = intent.Kind switch
            {
                AdvancedIntentKind.Rest => "rest",
                AdvancedIntentKind.Work => "work",
                AdvancedIntentKind.Solitude => "solitude",
                _ => "internal"
            };
            var ctx = new PerceptionContext(eventType, line, Mathf.Clamp(intent.Score, 0.35f, 0.85f), source.CharacterName);
            _enrichPerceptionFromOrchestrator(orchestrator, source, intent, ref ctx);
            source.TryEnqueuePerceive(ctx, null, false);
            _cooldowns[source] = Time.unscaledTime + Mathf.Max(0.5f, _nonSocialCooldownSeconds);

            if (_verboseLog)
            {
                Debug.Log($"[魂核/OpenBehavior] {source.CharacterName} 执行{intent.Kind}: {line}", this);
            }
        }

        private void _tryExecuteFallbackAfterSocialBlocked(AdvancedOrchestrator orchestrator, SoulNPC source, SoulNPC blockedTarget, SocializeBlockReason reason)
        {
            if (orchestrator == null || source == null || source.Soul == null) return;
            if (reason == SocializeBlockReason.GlobalLimit)
            {
                var wait = _getGlobalWindowRemainingSeconds() + 0.2f;
                _cooldowns[source] = Time.unscaledTime + wait;
                if (_verboseLog)
                {
                    var now = Time.unscaledTime;
                    if (!_nextGlobalLimitLogAt.TryGetValue(source, out var nextAt) || now >= nextAt)
                    {
                        _nextGlobalLimitLogAt[source] = now + 10f;
                        Debug.Log($"[魂核/OpenBehavior] {source.CharacterName} 社交受限({reason})，等待全局窗口释放约 {Mathf.Max(0f, wait):F1}s。", this);
                    }
                }
                return;
            }

            if (reason == SocializeBlockReason.PerPairLimit || reason == SocializeBlockReason.PerNpcLimit)
            {
                // PerPair/PerNpc 常见于“对话配额打满”，先短冷却并记录目标，促使下次换人。
                if (blockedTarget != null)
                {
                    _lastTargets[source] = blockedTarget;
                }
                var shortCooldown = reason == SocializeBlockReason.PerPairLimit ? 2.2f : _getPerNpcWindowRemainingSeconds(source) + 0.2f;
                _cooldowns[source] = Time.unscaledTime + shortCooldown;
                if (_verboseLog)
                {
                    if (reason == SocializeBlockReason.PerNpcLimit)
                    {
                        var now = Time.unscaledTime;
                        if (!_nextPerNpcLimitLogAt.TryGetValue(source, out var nextAt) || now >= nextAt)
                        {
                            _nextPerNpcLimitLogAt[source] = now + 10f;
                            Debug.Log($"[魂核/OpenBehavior] {source.CharacterName} 社交受限({reason})，等待窗口释放约 {Mathf.Max(0f, shortCooldown):F1}s。", this);
                        }
                    }
                    else
                    {
                        Debug.Log($"[魂核/OpenBehavior] {source.CharacterName} 社交受限({reason})，短冷却并尝试切换目标。", this);
                    }
                }
                return;
            }
            if (!orchestrator.TryGetDriveValues(source, out var social, out var rest, out var work, out var solitude)) return;
            var fallback = _chooseFallbackIntentAfterBlockedSocial(orchestrator, source, rest, work, solitude, reason);
            if (fallback.Kind == AdvancedIntentKind.None) return;
            _executeNonSocialIntent(orchestrator, source, fallback);
            _cooldowns[source] = Time.unscaledTime + Mathf.Max(0.5f, _blockedSocialFallbackCooldownSeconds);
            if (_verboseLog)
            {
                Debug.Log(
                    $"[魂核/OpenBehavior] {source.CharacterName} 社交受限({reason})，回退执行{fallback.Kind} score={fallback.Score:F2} " +
                    $"(social={social:F2}, rest={rest:F2}, work={work:F2}, solitude={solitude:F2})",
                    this);
            }
        }

        private AdvancedIntent _chooseFallbackIntentAfterBlockedSocial(
            AdvancedOrchestrator orchestrator,
            SoulNPC source,
            float rest,
            float work,
            float solitude,
            SocializeBlockReason reason)
        {
            // 社交被拦截后，默认不回退到 Rest，优先 Work/Solitude 做分流。
            var workBoost = reason == SocializeBlockReason.GlobalLimit ? 0.10f : 0.07f;
            var solitudeBoost = reason == SocializeBlockReason.PerPairLimit ? 0.10f : 0.07f;
            if (_linkOrchestratorSignals && orchestrator != null && source != null &&
                orchestrator.TryGetSignal(source, AdvancedSignalId.FePressure, out var fe))
            {
                solitudeBoost += fe * 0.08f;
            }
            var sWork = Mathf.Max(0f, work + workBoost);
            var sSolitude = Mathf.Max(0f, solitude + solitudeBoost);

            if (sWork < 0.12f && sSolitude < 0.12f)
            {
                // 极端低驱动才允许短暂回退 Rest，防止僵住。
                var sRestEmergency = Mathf.Max(0f, rest - 0.10f);
                if (sRestEmergency >= 0.10f)
                {
                    return new AdvancedIntent { Kind = AdvancedIntentKind.Rest, Score = sRestEmergency, Reason = "fallback.rest.emergency_after_social_blocked" };
                }
                return default;
            }

            if (sWork >= sSolitude)
            {
                return new AdvancedIntent { Kind = AdvancedIntentKind.Work, Score = sWork, Reason = "fallback.work.after_social_blocked" };
            }
            else
            {
                return new AdvancedIntent { Kind = AdvancedIntentKind.Solitude, Score = sSolitude, Reason = "fallback.solitude.after_social_blocked" };
            }
        }

        private static string _composeNonSocialLine(SoulNPC source, AdvancedIntent intent)
        {
            var t = DateTime.Now.Second + source.GetInstanceID();
            return intent.Kind switch
            {
                AdvancedIntentKind.Rest => RestPool[Mathf.Abs(t) % RestPool.Length],
                AdvancedIntentKind.Work => WorkPool[Mathf.Abs(t) % WorkPool.Length],
                AdvancedIntentKind.Solitude => SolitudePool[Mathf.Abs(t) % SolitudePool.Length],
                _ => null
            };
        }

        private static void _trimWindow(Queue<float> q, float now)
        {
            if (q == null) return;
            while (q.Count > 0 && now - q.Peek() > 60f)
            {
                q.Dequeue();
            }
        }

        private float _getPerNpcWindowRemainingSeconds(SoulNPC source)
        {
            if (source == null) return 3f;
            var now = Time.unscaledTime;
            var q = _getNpcQueue(source.GetInstanceID());
            _trimWindow(q, now);
            if (q.Count <= 0) return 3f;
            var oldest = q.Peek();
            return Mathf.Clamp((oldest + 60f) - now, 3f, 60f);
        }

        private float _getGlobalWindowRemainingSeconds()
        {
            var now = Time.unscaledTime;
            _trimWindow(_socializeTimestamps, now);
            if (_socializeTimestamps.Count <= 0) return 3f;
            var oldest = _socializeTimestamps.Peek();
            return Mathf.Clamp((oldest + 60f) - now, 3f, 60f);
        }

        private Queue<float> _getNpcQueue(int npcId)
        {
            if (!_perNpcSocializeTimestamps.TryGetValue(npcId, out var q))
            {
                q = new Queue<float>();
                _perNpcSocializeTimestamps[npcId] = q;
            }
            return q;
        }

        private Queue<float> _getPairQueue(int aId, int bId)
        {
            var key = _pairKey(aId, bId);
            if (!_perPairSocializeTimestamps.TryGetValue(key, out var q))
            {
                q = new Queue<float>();
                _perPairSocializeTimestamps[key] = q;
            }
            return q;
        }

        private static ulong _pairKey(int aId, int bId)
        {
            uint a = unchecked((uint)aId);
            uint b = unchecked((uint)bId);
            if (a > b) (a, b) = (b, a);
            return ((ulong)a << 32) | b;
        }

        public void ResetLimitStats()
        {
            _blockedByGlobalLimit = 0;
            _blockedByPerNpcLimit = 0;
            _blockedByPerPairLimit = 0;
            _socializeSuccessCount = 0;
        }

        [ContextMenu("应用人格倾向预设(当前引用)")]
        public void ApplyPersonalityPresetFromReference()
        {
            ApplyPersonalityPreset(_personalityPreset);
        }

        public void ApplyPersonalityPreset(OpenBehaviorPersonalityPreset preset)
        {
            if (preset == null)
            {
                if (_verboseLog) Debug.LogWarning("[魂核/OpenBehavior] 未指定人格倾向预设，已跳过应用。", this);
                return;
            }

            _personalityPreset = preset;
            _personalityAffectsSocialExpression = preset.personalityAffectsSocialExpression;
            _personalityAffectsTargetSelection = preset.personalityAffectsTargetSelection;
            _personalityTargetSelectionWeight = Mathf.Clamp01(preset.personalityTargetSelectionWeight);
            _personalityAffectsStylePrefix = preset.personalityAffectsStylePrefix;
            _personalityStyleWeight = Mathf.Clamp01(preset.personalityStyleWeight);
            _personalityAffectsTopicPreference = preset.personalityAffectsTopicPreference;
            _personalityTopicWeight = Mathf.Clamp01(preset.personalityTopicWeight);

            _taskPeriodPersonalityWeightOverride = preset.taskPeriodPersonalityWeightOverride;
            _taskPeriodTargetSelectionWeightMultiplier = Mathf.Clamp(preset.taskPeriodTargetSelectionWeightMultiplier, 0f, 2f);
            _taskPeriodStyleWeightMultiplier = Mathf.Clamp(preset.taskPeriodStyleWeightMultiplier, 0f, 2f);
            _taskPeriodTopicWeightMultiplier = Mathf.Clamp(preset.taskPeriodTopicWeightMultiplier, 0f, 2f);

            _urgentTaskExtraPersonalityWeightOverride = preset.urgentTaskExtraPersonalityWeightOverride;
            _urgentTaskTargetSelectionWeightMultiplier = Mathf.Clamp(preset.urgentTaskTargetSelectionWeightMultiplier, 0f, 2f);
            _urgentTaskStyleWeightMultiplier = Mathf.Clamp(preset.urgentTaskStyleWeightMultiplier, 0f, 2f);
            _urgentTaskTopicWeightMultiplier = Mathf.Clamp(preset.urgentTaskTopicWeightMultiplier, 0f, 2f);
        }

        public void ApplyAutoSuggestedTuning()
        {
            // 先根据窗口利用率判断“瓶颈在哪”，再做小步调整，避免过冲。
            GetWindowUtilization(out var globalPct, out var perNpcPct, out var perPairPct);

            if (globalPct >= 0.95f)
            {
                _maxSocializeEventsPerMinute = Mathf.Clamp(_maxSocializeEventsPerMinute + 2, 4, 60);
            }
            else if (globalPct <= 0.45f && _blockedByGlobalLimit < 10)
            {
                _maxSocializeEventsPerMinute = Mathf.Clamp(_maxSocializeEventsPerMinute - 1, 4, 60);
            }

            if (perNpcPct >= 0.95f && _blockedByPerNpcLimit > _blockedByPerPairLimit)
            {
                _maxSocializePerNpcPerMinute = Mathf.Clamp(_maxSocializePerNpcPerMinute + 1, 1, 5);
            }
            else if (perNpcPct <= 0.4f && _maxSocializePerNpcPerMinute > 1)
            {
                _maxSocializePerNpcPerMinute = Mathf.Clamp(_maxSocializePerNpcPerMinute - 1, 1, 5);
            }

            if (perPairPct >= 0.95f && _blockedByPerPairLimit > _blockedByPerNpcLimit)
            {
                _maxSocializePerPairPerMinute = Mathf.Clamp(_maxSocializePerPairPerMinute + 1, 1, 3);
            }
            else if (perPairPct <= 0.35f && _maxSocializePerPairPerMinute > 1)
            {
                _maxSocializePerPairPerMinute = Mathf.Clamp(_maxSocializePerPairPerMinute - 1, 1, 3);
            }

            if (_blockedByGlobalLimit > 1000)
            {
                _socializeCooldownSeconds = Mathf.Clamp(_socializeCooldownSeconds + 1f, 6f, 28f);
            }
            else if (globalPct < 0.55f && _socializeCooldownSeconds > 8f)
            {
                _socializeCooldownSeconds = Mathf.Clamp(_socializeCooldownSeconds - 0.5f, 6f, 28f);
            }
        }

        public void GetWindowUtilization(out float globalPct, out float perNpcMaxPct, out float perPairMaxPct)
        {
            var now = Time.unscaledTime;
            _trimWindow(_socializeTimestamps, now);
            globalPct = _percent(_socializeTimestamps.Count, Mathf.Max(1, _maxSocializeEventsPerMinute));

            var hottestNpcCount = _trimAndGetMaxCount(_perNpcSocializeTimestamps, now);
            perNpcMaxPct = _percent(hottestNpcCount, Mathf.Max(1, _maxSocializePerNpcPerMinute));

            var hottestPairCount = _trimAndGetMaxCount(_perPairSocializeTimestamps, now);
            perPairMaxPct = _percent(hottestPairCount, Mathf.Max(1, _maxSocializePerPairPerMinute));
        }

        private void _countBlocked(SocializeBlockReason reason)
        {
            switch (reason)
            {
                case SocializeBlockReason.GlobalLimit:
                    _blockedByGlobalLimit++;
                    break;
                case SocializeBlockReason.PerNpcLimit:
                    _blockedByPerNpcLimit++;
                    break;
                case SocializeBlockReason.PerPairLimit:
                    _blockedByPerPairLimit++;
                    break;
                default:
                    break;
            }
        }

        private static float _percent(int current, int limit)
        {
            if (limit <= 0) return 0f;
            return Mathf.Clamp01((float)current / limit) * 100f;
        }

        private static int _trimAndGetMaxCount<TKey>(Dictionary<TKey, Queue<float>> map, float now)
        {
            if (map == null || map.Count == 0) return 0;
            var max = 0;
            List<TKey> staleKeys = null;
            foreach (var kv in map)
            {
                var q = kv.Value;
                _trimWindow(q, now);
                if (q.Count == 0)
                {
                    if (staleKeys == null) staleKeys = new List<TKey>();
                    staleKeys.Add(kv.Key);
                    continue;
                }
                if (q.Count > max) max = q.Count;
            }

            if (staleKeys != null)
            {
                for (var i = 0; i < staleKeys.Count; i++)
                {
                    map.Remove(staleKeys[i]);
                }
            }
            return max;
        }

        private void _recordUrgentSilentHit(SoulNPC source)
        {
            if (source == null) return;
            var now = Time.unscaledTime;
            var q = _getUrgentSilentQueue(source.GetInstanceID());
            q.Enqueue(now);
            _trimWindow(q, now);
        }

        private int _urgentSilentHitsLastMinute(SoulNPC source)
        {
            if (source == null) return 0;
            var now = Time.unscaledTime;
            var q = _getUrgentSilentQueue(source.GetInstanceID());
            _trimWindow(q, now);
            return q.Count;
        }

        private Queue<float> _getUrgentSilentQueue(int npcId)
        {
            if (!_urgentSilentHitTimestamps.TryGetValue(npcId, out var q))
            {
                q = new Queue<float>();
                _urgentSilentHitTimestamps[npcId] = q;
            }
            return q;
        }
    }
}
