using System;
using System.Collections.Generic;
using System.Text;
using SoulCore.Advanced;
using UnityEngine;

namespace SoulCore
{
    /// <summary>
    /// 6.1.8 高级编排：按 <see cref="_updateHz"/> 评估各 NPC 的 <see cref="AdvancedNpcState"/>。
    /// 与 <see cref="OpenBehaviorDriver"/> 同帧时，建议在 Project Settings → Script Execution Order 中让本组件略早于驱动器，
    /// 以便 <see cref="TryGetSignal"/> / <see cref="TryGetIntent"/> 读到当帧已更新的 Signals。
    /// </summary>
    [AddComponentMenu("魂核/高级编排器 (6.1.8)")]
    [DisallowMultipleComponent]
    public class AdvancedOrchestrator : MonoBehaviour
    {
        [SerializeField, InspectorName("仅高级版生效"), Tooltip("仅在高级版生效，避免影响 4.0 主流程。")]
        private bool _requireProEdition = true;
        [SerializeField, Range(0.1f, 5f), InspectorName("更新频率(次/秒)"), Tooltip("高级模块评估频率（次/秒）。")]
        private float _updateHz = 2f;
        [SerializeField, Range(0f, 1f), InspectorName("社交触发阈值"), Tooltip("达到该阈值才触发社交意图。推荐 0.40~0.55（0.35更易触发，0.60更克制）。")]
        private float _socializeThreshold = 0.55f;
        [SerializeField, Range(0f, 1f), InspectorName("休息触发阈值"), Tooltip("达到该阈值时优先进入休息意图。")]
        private float _restThreshold = 0.62f;
        [SerializeField, Range(0f, 1f), InspectorName("工作触发阈值"), Tooltip("达到该阈值时进入工作意图。")]
        private float _workThreshold = 0.56f;
        [SerializeField, Range(0f, 1f), InspectorName("独处触发阈值"), Tooltip("达到该阈值时进入独处意图。")]
        private float _solitudeThreshold = 0.58f;
        [SerializeField, Range(0f, 1f), InspectorName("社交激活地板"), Tooltip("低于该值时，策略不会在接近并列/回退决策里优先社交；建议 0.40~0.46。")]
        private float _socialActivationFloor = 0.42f;
        [SerializeField, InspectorName("启用人格桥接"), Tooltip("将人格特质写入 personality_* 信号，并在策略层做轻微修正。")]
        private bool _enablePersonalityBridge = true;
        [SerializeField, InspectorName("人格桥接诊断日志"), Tooltip("默认关闭。开启后输出人格原始特质与聚合后的人格信号，排查人格是否被压平。")]
        private bool _personalityBridgeDebugLog;
        [SerializeField, Min(0.5f), InspectorName("人格诊断间隔(秒)"), Tooltip("建议 2~5 秒，过小会刷屏。")]
        private float _personalityBridgeDebugIntervalSeconds = 3f;
        [SerializeField, InspectorName("人格诊断名称过滤"), Tooltip("可选。仅打印名称包含该关键词的 NPC（不区分大小写），留空表示全部。")]
        private string _personalityBridgeDebugNameFilter = string.Empty;
        [SerializeField, InspectorName("启动后打印人格摘要"), Tooltip("默认关闭。运行后自动打印一次全场 NPC 的 psa/ptf/per 摘要（按 psa 降序）。")]
        private bool _logPersonalitySnapshotOnStart;
        [SerializeField, Min(1), InspectorName("人格摘要最多条数"), Tooltip("人格摘要最多打印多少条，避免大场景刷屏。")]
        private int _personalitySnapshotMaxRows = 20;
        [SerializeField, InspectorName("自动收集NPC"), Tooltip("默认扫描场景中所有 SoulNPC。")]
        private bool _autoCollectNpcs = true;
        [SerializeField, InspectorName("手动NPC列表")] private List<SoulNPC> _manualNpcs = new();

        public static AdvancedOrchestrator Instance { get; private set; }

        private readonly Dictionary<SoulNPC, AdvancedNpcState> _states = new();
        private readonly List<IAdvancedModule> _modules = new();
        private readonly PersonalityBridgeModule.DebugOptions _personalityBridgeDebugOptions = new();
        private PriorityPolicy9 _policy;
        private float _nextTickAt;
        private bool _loggedProGate;
        private bool _personalitySnapshotLogged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[魂核] 多个 AdvancedOrchestrator，保留首个实例。", this);
                Destroy(this);
                return;
            }
            Instance = this;
            _policy = new PriorityPolicy9();
            // 6.1.8 扩展：动机 → 意识状态 → 叙事自我 → 自由能 → 自指涉（后者依赖前几项写入的 Signals）
            _modules.Add(new MotivationModule());
            _modules.Add(new ConsciousnessStateModule());
            _modules.Add(new NarrativeSelfModule());
            _modules.Add(new FreeEnergyModule());
            _modules.Add(new SelfReferenceModule());
            _syncPersonalityBridgeDebugOptions();
            if (_enablePersonalityBridge) _modules.Add(new PersonalityBridgeModule(_personalityBridgeDebugOptions));
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_requireProEdition && !SoulCoreEdition.IsPro)
            {
                if (!_loggedProGate)
                {
                    Debug.LogWarning("[魂核/Advanced] 当前不是高级版（SOUL_EDITION_PRO），高级编排器未运行。", this);
                    _loggedProGate = true;
                }
                return;
            }
            if (Time.unscaledTime < _nextTickAt) return;
            _nextTickAt = Time.unscaledTime + 1f / Mathf.Max(0.1f, _updateHz);

            _syncNpcList();
            _evaluateAll();
            if (_logPersonalitySnapshotOnStart && !_personalitySnapshotLogged)
            {
                _personalitySnapshotLogged = true;
                DumpPersonalitySnapshot();
            }
        }

        /// <summary>上一帧 <see cref="_evaluateAll"/> 结束后缓存的意图（含 <see cref="AdvancedIntent.Reason"/>）。</summary>
        public bool TryGetIntent(SoulNPC npc, out AdvancedIntent intent)
        {
            intent = default;
            if (npc == null) return false;
            if (!_states.TryGetValue(npc, out var state) || state == null) return false;
            intent = state.Intent;
            return true;
        }

        /// <summary>读取 <see cref="AdvancedNpcState.Drives"/> 中四路驱动（键名 <see cref="AdvancedDriveKeys"/>；可能含策略回写后的本帧有效值）。</summary>
        public bool TryGetDriveValues(SoulNPC npc, out float social, out float rest, out float work, out float solitude)
        {
            social = 0f;
            rest = 0f;
            work = 0f;
            solitude = 0f;
            if (npc == null) return false;
            if (!_states.TryGetValue(npc, out var state) || state == null || state.Drives == null) return false;
            social = _getDrive(state, AdvancedDriveKeys.Socialize);
            rest = _getDrive(state, AdvancedDriveKeys.Rest);
            work = _getDrive(state, AdvancedDriveKeys.Work);
            solitude = _getDrive(state, AdvancedDriveKeys.Solitude);
            return true;
        }

        /// <summary>
        /// 读取本 NPC 当前 <see cref="AdvancedNpcState.Signals"/>（动态键时使用字符串；优先 <see cref="TryGetSignal(SoulNPC, AdvancedSignalId, out float)"/>）。
        /// </summary>
        public bool TryGetSignal(SoulNPC npc, string key, out float value)
        {
            value = 0f;
            if (npc == null || string.IsNullOrEmpty(key)) return false;
            if (!_states.TryGetValue(npc, out var state) || state == null || state.Signals == null) return false;
            if (!state.Signals.TryGetValue(key, out value)) return false;
            value = Mathf.Clamp01(value);
            return true;
        }

        /// <inheritdoc cref="TryGetSignal(SoulNPC, string, out float)"/>
        public bool TryGetSignal(SoulNPC npc, AdvancedSignalId signalId, out float value) =>
            TryGetSignal(npc, AdvancedSignalIdTable.Key(signalId), out value);

        /// <summary>读取单路 <see cref="AdvancedNpcState.Drives"/>（键名 <see cref="AdvancedDriveKeys"/>）。</summary>
        public bool TryGetDrive(SoulNPC npc, AdvancedDriveId driveId, out float value)
        {
            value = 0f;
            if (npc == null) return false;
            if (!_states.TryGetValue(npc, out var state) || state == null || state.Drives == null) return false;
            var key = AdvancedDriveIdTable.Key(driveId);
            if (!state.Drives.TryGetValue(key, out value)) return false;
            value = Mathf.Clamp01(value);
            return true;
        }

        private void _syncNpcList()
        {
            var set = new HashSet<SoulNPC>();
            if (_autoCollectNpcs)
            {
                var npcs = FindObjectsOfType<SoulNPC>();
                for (var i = 0; i < npcs.Length; i++)
                {
                    var n = npcs[i];
                    if (n == null || n.Soul == null) continue;
                    set.Add(n);
                }
            }

            for (var i = 0; i < _manualNpcs.Count; i++)
            {
                var n = _manualNpcs[i];
                if (n == null || n.Soul == null) continue;
                set.Add(n);
            }

            var toRemove = new List<SoulNPC>();
            foreach (var kv in _states)
            {
                if (!set.Contains(kv.Key)) toRemove.Add(kv.Key);
            }
            for (var i = 0; i < toRemove.Count; i++) _states.Remove(toRemove[i]);

            foreach (var n in set)
            {
                if (_states.ContainsKey(n)) continue;
                _states[n] = new AdvancedNpcState { Npc = n };
            }
        }

        private void _evaluateAll()
        {
            var dt = 1f / Mathf.Max(0.1f, _updateHz);
            foreach (var kv in _states)
            {
                var state = kv.Value;
                if (state == null || state.Npc == null || state.Npc.Soul == null) continue;

                state.Drives.Clear();
                state.Signals.Clear();

                for (var i = 0; i < _modules.Count; i++)
                {
                    _modules[i].Tick(state, dt);
                }

                state.Intent = _policy.Evaluate(
                    state,
                    _socializeThreshold,
                    _restThreshold,
                    _workThreshold,
                    _solitudeThreshold,
                    _socialActivationFloor);
            }
        }

        private static float _getDrive(AdvancedNpcState state, string key)
        {
            if (state == null || state.Drives == null || string.IsNullOrEmpty(key)) return 0f;
            return state.Drives.TryGetValue(key, out var v) ? Mathf.Clamp01(v) : 0f;
        }

        private void _syncPersonalityBridgeDebugOptions()
        {
            _personalityBridgeDebugOptions.Enabled = _personalityBridgeDebugLog;
            _personalityBridgeDebugOptions.IntervalSeconds = Mathf.Max(0.5f, _personalityBridgeDebugIntervalSeconds);
            _personalityBridgeDebugOptions.NpcNameContains = _personalityBridgeDebugNameFilter;
        }

        private void OnValidate()
        {
            _syncPersonalityBridgeDebugOptions();
        }

        [ContextMenu("打印人格摘要快照")]
        public void DumpPersonalitySnapshot()
        {
            var rows = new List<(string name, float psa, float ptf, float per, float social, float work, float solitude)>();
            foreach (var kv in _states)
            {
                var npc = kv.Key;
                var state = kv.Value;
                if (npc == null || state == null) continue;
                var name = string.IsNullOrEmpty(npc.CharacterName) ? npc.name : npc.CharacterName;
                var psa = _getSignal(state, AdvancedSignalKeys.PersonalitySocialApproach);
                var ptf = _getSignal(state, AdvancedSignalKeys.PersonalityTaskFocus);
                var per = _getSignal(state, AdvancedSignalKeys.PersonalityEmotionalReactivity);
                var social = _getDrive(state, AdvancedDriveKeys.Socialize);
                var work = _getDrive(state, AdvancedDriveKeys.Work);
                var solitude = _getDrive(state, AdvancedDriveKeys.Solitude);
                rows.Add((name, psa, ptf, per, social, work, solitude));
            }

            if (rows.Count == 0)
            {
                Debug.Log("[魂核/Advanced][人格摘要] 当前无可用 NPC 数据。", this);
                return;
            }

            rows.Sort((a, b) => b.psa.CompareTo(a.psa));
            var maxRows = Mathf.Clamp(_personalitySnapshotMaxRows, 1, 999);
            var take = Mathf.Min(maxRows, rows.Count);
            var sb = new StringBuilder(256);
            sb.Append($"[魂核/Advanced][人格摘要] count={rows.Count} showing={take} sort=psa_desc");
            for (var i = 0; i < take; i++)
            {
                var r = rows[i];
                sb.Append("\n");
                sb.Append($"{i + 1,2}. {r.name} ");
                sb.Append($"psa={r.psa:F2} ptf={r.ptf:F2} per={r.per:F2} ");
                sb.Append($"| social={r.social:F2} work={r.work:F2} solitude={r.solitude:F2}");
            }

            Debug.Log(sb.ToString(), this);
        }

        private static float _getSignal(AdvancedNpcState state, string key)
        {
            if (state == null || state.Signals == null || string.IsNullOrEmpty(key)) return 0f;
            return state.Signals.TryGetValue(key, out var v) ? Mathf.Clamp01(v) : 0f;
        }
    }
}
