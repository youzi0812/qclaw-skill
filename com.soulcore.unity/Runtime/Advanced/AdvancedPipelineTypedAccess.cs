using System;

namespace SoulCore.Advanced
{
    /// <summary>
    /// 与 <see cref="AdvancedSignalKeys"/> 一一对应，供 <see cref="AdvancedOrchestrator.TryGetSignal(SoulCore.SoulNPC, AdvancedSignalId, out float)"/> 等强类型入口，减少拼写错误。
    /// </summary>
    public enum AdvancedSignalId
    {
        Loneliness,
        Missing,
        SocialBase,
        SocialWave,
        SocialNoise,
        SocialMomentum,
        ClockHour,
        LateNight,
        DailyInteractions01,
        ConsciousnessArousal,
        ConsciousnessClarity,
        ConsciousnessLoad,
        ConsciousnessDrift,
        NarrativeCoherence,
        NarrativeTension,
        NarrativeStance,
        NarrativeMemory01,
        FePredictionError,
        FeUncertainty,
        FePressure,
        SelfContinuity,
        SelfMetaLoad,
        SelfReflexiveTension,
        PersonalitySocialApproach,
        PersonalityTaskFocus,
        PersonalityEmotionalReactivity
    }

    public static class AdvancedSignalIdTable
    {
        public static string Key(AdvancedSignalId id) => id switch
        {
            AdvancedSignalId.Loneliness => AdvancedSignalKeys.Loneliness,
            AdvancedSignalId.Missing => AdvancedSignalKeys.Missing,
            AdvancedSignalId.SocialBase => AdvancedSignalKeys.SocialBase,
            AdvancedSignalId.SocialWave => AdvancedSignalKeys.SocialWave,
            AdvancedSignalId.SocialNoise => AdvancedSignalKeys.SocialNoise,
            AdvancedSignalId.SocialMomentum => AdvancedSignalKeys.SocialMomentum,
            AdvancedSignalId.ClockHour => AdvancedSignalKeys.ClockHour,
            AdvancedSignalId.LateNight => AdvancedSignalKeys.LateNight,
            AdvancedSignalId.DailyInteractions01 => AdvancedSignalKeys.DailyInteractions01,
            AdvancedSignalId.ConsciousnessArousal => AdvancedSignalKeys.ConsciousnessArousal,
            AdvancedSignalId.ConsciousnessClarity => AdvancedSignalKeys.ConsciousnessClarity,
            AdvancedSignalId.ConsciousnessLoad => AdvancedSignalKeys.ConsciousnessLoad,
            AdvancedSignalId.ConsciousnessDrift => AdvancedSignalKeys.ConsciousnessDrift,
            AdvancedSignalId.NarrativeCoherence => AdvancedSignalKeys.NarrativeCoherence,
            AdvancedSignalId.NarrativeTension => AdvancedSignalKeys.NarrativeTension,
            AdvancedSignalId.NarrativeStance => AdvancedSignalKeys.NarrativeStance,
            AdvancedSignalId.NarrativeMemory01 => AdvancedSignalKeys.NarrativeMemory01,
            AdvancedSignalId.FePredictionError => AdvancedSignalKeys.FePredictionError,
            AdvancedSignalId.FeUncertainty => AdvancedSignalKeys.FeUncertainty,
            AdvancedSignalId.FePressure => AdvancedSignalKeys.FePressure,
            AdvancedSignalId.SelfContinuity => AdvancedSignalKeys.SelfContinuity,
            AdvancedSignalId.SelfMetaLoad => AdvancedSignalKeys.SelfMetaLoad,
            AdvancedSignalId.SelfReflexiveTension => AdvancedSignalKeys.SelfReflexiveTension,
            AdvancedSignalId.PersonalitySocialApproach => AdvancedSignalKeys.PersonalitySocialApproach,
            AdvancedSignalId.PersonalityTaskFocus => AdvancedSignalKeys.PersonalityTaskFocus,
            AdvancedSignalId.PersonalityEmotionalReactivity => AdvancedSignalKeys.PersonalityEmotionalReactivity,
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
        };
    }

    /// <summary>与 <see cref="AdvancedDriveKeys"/> 一一对应。</summary>
    public enum AdvancedDriveId
    {
        Socialize,
        Rest,
        Work,
        Solitude
    }

    public static class AdvancedDriveIdTable
    {
        public static string Key(AdvancedDriveId id) => id switch
        {
            AdvancedDriveId.Socialize => AdvancedDriveKeys.Socialize,
            AdvancedDriveId.Rest => AdvancedDriveKeys.Rest,
            AdvancedDriveId.Work => AdvancedDriveKeys.Work,
            AdvancedDriveId.Solitude => AdvancedDriveKeys.Solitude,
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
        };
    }
}
