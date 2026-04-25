using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoulCore
{
    // 始终挂到「魂核/」下，团结工程未加 SOULCORE_CHANNEL_TUANJIE 时也能在中文菜单里找到；用 Add Component 搜 "Soul" / "NPC" 亦可
    [AddComponentMenu("魂核/魂核 NPC（意识流）")]
    [DisallowMultipleComponent]
    public class SoulNPC : MonoBehaviour
    {
        [SerializeField] private string _citizenId;
        [SerializeField] private string _displayName;
        [SerializeField] private int _randomSeed;
        [SerializeField] private SoulNPCConfig _config;
        [Tooltip("不填则使用 Resources/SoulOpenAgentDefault；用于接入 HTTP 智能体时读取 URL/模型/密钥等。")]
        [SerializeField] private SoulOpenAgentProfile _openAgentOverride;

        [SerializeField] private float _secondsPerGameDay = 300f;
        [SerializeField] private bool _runDailyAutoReset = true;
        [SerializeField] private bool _dailyResetOnlyInRange = true;

        [SerializeField] [Range(0f, 1f)] private float _resourceScarcity = 0.5f;
        [SerializeField] private bool _useScarcitySliderAsProvider = true;

        [SerializeField] private float _maxDistanceMeters;
        [SerializeField] private Transform _focusTarget;
        [SerializeField] private float _maxPerceptionsPerSecond = 5f;
        [SerializeField] private bool _enforceProcessingBudget = true;

        [SerializeField] private SoulProcessScheduler _processScheduler;
        [SerializeField] private bool _autoEnsureScheduler;

        [SerializeField] private SoulFlavorOutputMode _flavorOutput = SoulFlavorOutputMode.BuiltInTemplates;

        [SerializeField] private bool _showDebug;
        [SerializeField] private string _debugStatus;

        public Soul Soul { get; private set; }
        public SoulDecision LastDecision { get; private set; }
        public string LastSkipReason { get; private set; }
        public bool IsBlockedByFreeEdition { get; private set; }

        /// <summary>与 <see cref="Soul"/> 与决策台本中使用的角色名一致；Play 时为 Soul.Name。调 UI/日志时请优先用此，而非仅 GameObject 名。</summary>
        public string CharacterName => Soul != null
            ? Soul.Name
            : (string.IsNullOrEmpty(_displayName) ? gameObject.name : _displayName);

        /// <summary>若指定，则 <see cref="SoulOpenAgent.ResolveFor"/> 优选用此配置，不读 Resources 默认资源。</summary>
        public SoulOpenAgentProfile OpenAgentOverride => _openAgentOverride;

        private float _nextPerceiveAllowedTime;
        private Coroutine _dailyRoutine;
        private bool _registeredWithEditionRegistry;

        private void Reset()
        {
            _displayName = gameObject.name;
        }

        private void Awake()
        {
            if (string.IsNullOrEmpty(_citizenId)) _citizenId = "npc_" + GetInstanceID();
            if (string.IsNullOrEmpty(_displayName))
            {
                if (_config != null && !string.IsNullOrEmpty(_config.suggestedDisplayName))
                    _displayName = _config.suggestedDisplayName;
                else
                    _displayName = gameObject.name;
            }
            if (SoulCoreEdition.IsFree)
            {
                if (!SoulNpcsActiveRegistry.TryRegister(this))
                {
                    IsBlockedByFreeEdition = true;
                    SoulFreeEditionUi.Notify(SoulFreeEditionNotifyKind.NpcCountExceeded);
                    return;
                }
                _registeredWithEditionRegistry = true;
            }
            if (_autoEnsureScheduler) SoulProcessScheduler.EnsureInHierarchy();
            var seed = _randomSeed;
            if (seed == 0) seed = System.Environment.TickCount;
            Soul = new Soul(_citizenId, _displayName, seed);
            if (_config != null)
            {
                _config.ToRuntimeParams().ApplyTo(Soul);
                _config.ApplyInitialRelationships(Soul);
            }
            else new SoulRuntimeParams { FlavorOutput = _flavorOutput }.ApplyTo(Soul);
            if (_useScarcitySliderAsProvider) Soul.ResourceScarcityProvider = () => _resourceScarcity;
        }

        private void OnEnable()
        {
            if (_runDailyAutoReset) _dailyRoutine = StartCoroutine(_DailyLoop());
        }

        private void OnDisable()
        {
            if (_dailyRoutine != null)
            {
                StopCoroutine(_dailyRoutine);
                _dailyRoutine = null;
            }
        }

        private void OnDestroy()
        {
            if (_registeredWithEditionRegistry) SoulNpcsActiveRegistry.Unregister(this);
            Soul?.Shutdown();
        }

        private IEnumerator _DailyLoop()
        {
            var wait = new WaitForSeconds(_secondsPerGameDay);
            while (enabled && Soul != null && Soul.IsRunning())
            {
                yield return wait;
                if (!_dailyResetOnlyInRange || IsWithinDistance()) Soul.DailyReset();
            }
        }

        private bool IsWithinDistance()
        {
            if (_maxDistanceMeters <= 0f) return true;
            if (_focusTarget == null) return true;
            return Vector3.SqrMagnitude(transform.position - _focusTarget.position) <= _maxDistanceMeters * _maxDistanceMeters;
        }

        private bool CanProcessPerceptionNow()
        {
            if (!IsWithinDistance())
            {
                LastSkipReason = "out_of_range";
                return false;
            }
            if (_maxPerceptionsPerSecond <= 0f)
            {
                LastSkipReason = null;
                return true;
            }
            if (Time.unscaledTime < _nextPerceiveAllowedTime)
            {
                LastSkipReason = "throttled";
                return false;
            }
            _nextPerceiveAllowedTime = Time.unscaledTime + 1f / _maxPerceptionsPerSecond;
            LastSkipReason = null;
            return true;
        }

        public SoulDecision Perceive(PerceptionContext context, bool bypassBudget = false)
        {
            if (IsBlockedByFreeEdition) return null;
            if (Soul == null) return null;
            if (_enforceProcessingBudget && !bypassBudget && !CanProcessPerceptionNow())
            {
                var skip = new SoulDecision
                {
                    Action = LastSkipReason == "out_of_range" ? "skipped_out_of_range" : "skipped_throttle",
                    Explanation = LastSkipReason == "out_of_range"
                        ? SoulCoreLocalization.Rt(SoulCoreI18nKeys.RtSkipOutOfRange)
                        : SoulCoreLocalization.Rt(SoulCoreI18nKeys.RtSkipThrottle),
                    Confidence = 0f,
                    Emotion = "—",
                    EmotionIntensity = 0f,
                    RelevantMemories = new List<Memory>()
                };
                LastDecision = skip;
                if (_showDebug) _debugStatus = LastSkipReason;
                return skip;
            }

            LastDecision = Soul.Perceive(context);
            if (_showDebug)
            {
                Soul.Memory.Stats.TryGetValue("total", out var memTotal);
                _debugStatus = $"emo={LastDecision.Emotion} act={LastDecision.Action} mem={memTotal}";
            }
            return LastDecision;
        }

        private SoulProcessScheduler _resolveScheduler() =>
            _processScheduler != null ? _processScheduler : SoulProcessScheduler.Instance;

        /// <summary>将一次感知排到分帧调度器，在后续帧内回调。调度器或 NPC 不可用时返回 false（请改用同步 <see cref="Perceive"/>）。</summary>
        public bool TryEnqueuePerceive(
            PerceptionContext context,
            Action<SoulDecision> onComplete = null,
            bool bypassBudget = false)
        {
            if (IsBlockedByFreeEdition) { onComplete?.Invoke(null); return false; }
            var sch = _resolveScheduler();
            if (sch == null) return false;
            sch.Enqueue(this, context, bypassBudget, onComplete);
            return true;
        }
    }
}
