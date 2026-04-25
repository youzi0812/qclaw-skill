using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SoulCore.Advanced
{
    /// <summary>
    /// 6.1.8 — 人格桥接 v0.1：将 PersonalityEngine traits 压缩为少量可消费信号（只写 Signals，不直接改 Drives）。
    /// </summary>
    public sealed class PersonalityBridgeModule : IAdvancedModule
    {
        public sealed class DebugOptions
        {
            public bool Enabled;
            public float IntervalSeconds = 3f;
            public string NpcNameContains;
        }

        public string ModuleId => "personality_bridge.v0.1";
        private readonly DebugOptions _debug;
        private readonly Dictionary<int, float> _nextLogAtByNpcId = new();

        public PersonalityBridgeModule(DebugOptions debug = null)
        {
            _debug = debug;
        }

        public void Tick(AdvancedNpcState state, float dt)
        {
            if (state == null || state.Npc == null || state.Npc.Soul == null || state.Npc.Soul.Personality == null) return;
            var p = state.Npc.Soul.Personality;
            var warmth = p.GetTrait(PersonalityEngine.TraitWarmth);
            var energy = p.GetTrait(PersonalityEngine.TraitEnergy);
            var trust = p.GetTrait(PersonalityEngine.TraitTrust);
            var persistence = p.GetTrait(PersonalityEngine.TraitPersistence);
            var rationality = p.GetTrait(PersonalityEngine.TraitRationality);
            var patience = p.GetTrait(PersonalityEngine.TraitPatience);
            var fearTendency = p.GetTrait(PersonalityEngine.TraitFearTendency);
            var angerTendency = p.GetTrait(PersonalityEngine.TraitAngerTendency);
            var sadnessTendency = p.GetTrait(PersonalityEngine.TraitSadnessTendency);
            var resilience = p.GetTrait(PersonalityEngine.TraitResilience);

            var socialApproach = Mathf.Clamp01(
                warmth * 0.35f +
                energy * 0.40f +
                trust * 0.25f);

            var taskFocus = Mathf.Clamp01(
                persistence * 0.45f +
                rationality * 0.35f +
                patience * 0.20f);

            var emotionalReactivity = Mathf.Clamp01(
                fearTendency * 0.45f +
                angerTendency * 0.20f +
                sadnessTendency * 0.20f +
                (1f - resilience) * 0.15f);

            state.Signals[AdvancedSignalKeys.PersonalitySocialApproach] = socialApproach;
            state.Signals[AdvancedSignalKeys.PersonalityTaskFocus] = taskFocus;
            state.Signals[AdvancedSignalKeys.PersonalityEmotionalReactivity] = emotionalReactivity;

            _tryLogDebug(
                state,
                warmth, energy, trust,
                persistence, rationality, patience,
                fearTendency, angerTendency, sadnessTendency, resilience,
                socialApproach, taskFocus, emotionalReactivity);
        }

        private void _tryLogDebug(
            AdvancedNpcState state,
            float warmth, float energy, float trust,
            float persistence, float rationality, float patience,
            float fearTendency, float angerTendency, float sadnessTendency, float resilience,
            float socialApproach, float taskFocus, float emotionalReactivity)
        {
            if (_debug == null || !_debug.Enabled || state == null || state.Npc == null) return;
            var npcName = state.Npc.CharacterName ?? state.Npc.name ?? string.Empty;
            var filter = _debug.NpcNameContains;
            if (!string.IsNullOrEmpty(filter) &&
                npcName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            var now = Time.unscaledTime;
            var id = state.Npc.GetInstanceID();
            if (_nextLogAtByNpcId.TryGetValue(id, out var nextAt) && now < nextAt) return;
            _nextLogAtByNpcId[id] = now + Mathf.Max(0.5f, _debug.IntervalSeconds);

            Debug.Log(
                "[魂核/Advanced][人格桥接诊断] " + npcName + " " +
                $"raw(w={_f2(warmth)} e={_f2(energy)} t={_f2(trust)} " +
                $"p={_f2(persistence)} r={_f2(rationality)} pa={_f2(patience)} " +
                $"fear={_f2(fearTendency)} anger={_f2(angerTendency)} sad={_f2(sadnessTendency)} res={_f2(resilience)}) " +
                $"-> agg(psa={_f2(socialApproach)} ptf={_f2(taskFocus)} per={_f2(emotionalReactivity)})");
        }

        private static string _f2(float value)
        {
            return value.ToString("F2", CultureInfo.InvariantCulture);
        }
    }
}
