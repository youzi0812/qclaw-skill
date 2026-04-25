using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SoulCore
{
    public sealed class SoulRuntimeParams
    {
        public string PersonalityPreset = "default";
        public int MaxMemories = 200;
        public float EmotionDailyDecayScale = 1f;
        public float DecisionPersonalityWeight = 0.25f;
        public float DecisionEmotionWeight = 0.25f;
        public float DecisionMemoryWeight = 0.20f;
        public float DecisionResourceWeight = 0.30f;
        public SoulModuleFlags Modules = SoulModuleFlags.AllEnabled;
        public SoulFlavorOutputMode FlavorOutput = SoulFlavorOutputMode.BuiltInTemplates;
        public Dictionary<string, float> PersonalityOverrides = null;
        public bool AutoDiversifyDefaultPersonality = true;
        public float AutoDiversifyStrength = 0.10f;

        public void ApplyTo(Soul soul)
        {
            if (soul == null) return;
            soul.Modules = Modules;
            soul.FlavorOutput = FlavorOutput;
            soul.Memory.SetMaxMemories(MaxMemories);
            if (!string.IsNullOrEmpty(PersonalityPreset) && PersonalityPreset != "default")
            {
                var pe = PersonalityEngine.CreatePreset(PersonalityPreset);
                foreach (var kv in pe.Traits) soul.Personality.Traits[kv.Key] = kv.Value;
            }
            if (PersonalityOverrides != null)
            {
                foreach (var kv in PersonalityOverrides)
                {
                    if (!soul.Personality.Traits.ContainsKey(kv.Key)) continue;
                    soul.Personality.Traits[kv.Key] = Math.Clamp(kv.Value, 0f, 1f);
                }
            }
            else if (AutoDiversifyDefaultPersonality &&
                     (string.IsNullOrEmpty(PersonalityPreset) || PersonalityPreset == "default"))
            {
                _applyDefaultPersonalityDiversification(soul, Math.Clamp(AutoDiversifyStrength, 0f, 0.35f));
            }
            soul.EmotionDailyDecayScale = EmotionDailyDecayScale;
            soul.Decision.Weights["personality"] = DecisionPersonalityWeight;
            soul.Decision.Weights["emotion"] = DecisionEmotionWeight;
            soul.Decision.Weights["memory"] = DecisionMemoryWeight;
            soul.Decision.Weights["resource"] = DecisionResourceWeight;
        }

        private static void _applyDefaultPersonalityDiversification(Soul soul, float strength)
        {
            if (soul == null || soul.Personality == null || strength <= 0f) return;

            // 仅对桥接会直接读取的 trait 做轻量扰动，确保默认人格也能区分 NPC。
            _jitterTrait(soul, PersonalityEngine.TraitWarmth, strength);
            _jitterTrait(soul, PersonalityEngine.TraitEnergy, strength);
            _jitterTrait(soul, PersonalityEngine.TraitTrust, strength);
            _jitterTrait(soul, PersonalityEngine.TraitPersistence, strength);
            _jitterTrait(soul, PersonalityEngine.TraitRationality, strength);
            _jitterTrait(soul, PersonalityEngine.TraitPatience, strength);
            _jitterTrait(soul, PersonalityEngine.TraitFearTendency, strength * 0.8f);
            _jitterTrait(soul, PersonalityEngine.TraitAngerTendency, strength * 0.7f);
            _jitterTrait(soul, PersonalityEngine.TraitSadnessTendency, strength * 0.7f);
            _jitterTrait(soul, PersonalityEngine.TraitResilience, strength);
        }

        private static void _jitterTrait(Soul soul, string traitName, float amplitude)
        {
            if (!soul.Personality.Traits.TryGetValue(traitName, out var baseValue)) return;
            var offset = (_stableUnit(soul.Id + "|" + traitName) - 0.5f) * 2f * amplitude;
            soul.Personality.Traits[traitName] = Math.Clamp(baseValue + offset, 0f, 1f);
        }

        private static float _stableUnit(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0.5f;
            byte[] bytes;
            using (var sha = SHA256.Create())
            {
                bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
            }
            var value = BitConverter.ToUInt32(bytes, 0);
            return value / (float)uint.MaxValue;
        }
    }
}
