namespace SoulCore.Advanced
{
    /// <summary>
    /// 6.1.8 高级管线：<see cref="AdvancedNpcState.Drives"/> 键名。
    /// 由 <see cref="MotivationModule"/> 写入，<see cref="PriorityPolicy9"/> 读取并可能回写本帧有效值。
    /// </summary>
    public static class AdvancedDriveKeys
    {
        public const string Socialize = "socialize";
        public const string Rest = "rest";
        public const string Work = "work";
        public const string Solitude = "solitude";
    }

    /// <summary>
    /// 6.1.8 高级管线：<see cref="AdvancedNpcState.Signals"/> 键名（各 <see cref="IAdvancedModule"/> 写入）。
    /// 重命名会破坏与下游（如 <see cref="UnityEngine.MonoBehaviour"/> 感知 Metadata、外部工具）的契约，请谨慎。
    /// </summary>
    public static class AdvancedSignalKeys
    {
        // MotivationModule
        public const string Loneliness = "loneliness";
        public const string Missing = "missing";
        public const string SocialBase = "social_base";
        public const string SocialWave = "social_wave";
        public const string SocialNoise = "social_noise";
        public const string SocialMomentum = "social_momentum";
        public const string ClockHour = "clock_hour";
        public const string LateNight = "late_night";
        public const string DailyInteractions01 = "daily_interactions_01";

        // ConsciousnessStateModule
        public const string ConsciousnessArousal = "consciousness_arousal";
        public const string ConsciousnessClarity = "consciousness_clarity";
        public const string ConsciousnessLoad = "consciousness_load";
        public const string ConsciousnessDrift = "consciousness_drift";

        // NarrativeSelfModule
        public const string NarrativeCoherence = "narrative_coherence";
        public const string NarrativeTension = "narrative_tension";
        public const string NarrativeStance = "narrative_stance";
        public const string NarrativeMemory01 = "narrative_memory_01";

        // FreeEnergyModule
        public const string FePredictionError = "fe_prediction_error";
        public const string FeUncertainty = "fe_uncertainty";
        public const string FePressure = "fe_pressure";

        // SelfReferenceModule
        public const string SelfContinuity = "self_continuity";
        public const string SelfMetaLoad = "self_meta_load";
        public const string SelfReflexiveTension = "self_reflexive_tension";

        // PersonalityBridgeModule
        public const string PersonalitySocialApproach = "personality_social_approach";
        public const string PersonalityTaskFocus = "personality_task_focus";
        public const string PersonalityEmotionalReactivity = "personality_emotional_reactivity";
    }

    /// <summary>
    /// 开放式行为驱动器写入 <c>PerceptionContext.Metadata</c> 的键名约定（命名空间 SoulCore）。
    /// </summary>
    public static class OpenBehaviorPerceptionMetadataKeys
    {
        public const string AdvancedIntentKind = "advanced_intent_kind";
        public const string AdvancedIntentReason = "advanced_intent_reason";

        /// <summary>与 <see cref="AdvancedSignalKeys"/> 中键对应，值为不变文化数值字符串（如 F3）。</summary>
        public static string SignalSnapshot(string pipelineSignalKey) => "signal_" + pipelineSignalKey;

        /// <inheritdoc cref="SignalSnapshot(string)"/>
        public static string SignalSnapshot(AdvancedSignalId id) => SignalSnapshot(AdvancedSignalIdTable.Key(id));
    }
}
