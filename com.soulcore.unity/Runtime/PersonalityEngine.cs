using System;
using System.Collections.Generic;

namespace SoulCore
{
    public class PersonalityEngine
    {
        public const string TraitWarmth = "warmth";
        public const string TraitCompassion = "compassion";
        public const string TraitTrust = "trust";
        public const string TraitOptimism = "optimism";
        public const string TraitEnergy = "energy";
        public const string TraitCourage = "courage";
        public const string TraitPatience = "patience";
        public const string TraitPersistence = "persistence";
        public const string TraitRationality = "rationality";
        public const string TraitCuriosity = "curiosity";
        public const string TraitCreativity = "creativity";
        public const string TraitHonesty = "honesty";
        public const string TraitJustice = "justice";
        public const string TraitLoyalty = "loyalty";
        public const string TraitSelfishness = "selfishness";
        public const string TraitFearTendency = "fear_tendency";
        public const string TraitAngerTendency = "anger_tendency";
        public const string TraitSadnessTendency = "sadness_tendency";
        public const string TraitHopeTendency = "hope_tendency";
        public const string TraitIntuition = "intuition";
        public const string TraitResilience = "resilience";

        public readonly Dictionary<string, float> Traits = new Dictionary<string, float>
        {
            { TraitWarmth, 0.5f },
            { TraitCompassion, 0.5f },
            { TraitTrust, 0.5f },
            { TraitOptimism, 0.5f },
            { TraitEnergy, 0.5f },
            { TraitCourage, 0.5f },
            { TraitPatience, 0.5f },
            { TraitPersistence, 0.5f },
            { TraitRationality, 0.5f },
            { TraitCuriosity, 0.5f },
            { TraitCreativity, 0.5f },
            { TraitHonesty, 0.5f },
            { TraitJustice, 0.5f },
            { TraitLoyalty, 0.5f },
            { TraitSelfishness, 0.5f },
            { TraitFearTendency, 0.2f },
            { TraitAngerTendency, 0.2f },
            { TraitSadnessTendency, 0.2f },
            { TraitHopeTendency, 0.6f },
            { TraitIntuition, 0.5f },
            { TraitResilience, 0.5f }
        };

        public float Stability = 0.8f;
        public double FormedAt;

        public PersonalityEngine()
        {
            FormedAt = GetNow();
        }

        private static double GetNow() => (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

        public float GetTrait(string name, float defaultValue = 0.5f) =>
            Traits.TryGetValue(name, out var v) ? v : defaultValue;

        public void SetTrait(string name, float value)
        {
            if (Traits.ContainsKey(name))
                Traits[name] = Math.Clamp(value, 0f, 1f);
        }

        public void FromDict(Dictionary<string, float> data)
        {
            foreach (var kv in data)
            {
                if (Traits.ContainsKey(kv.Key))
                    Traits[kv.Key] = Math.Clamp(kv.Value, 0f, 1f);
            }
        }

        public string Describe()
        {
            if (GetTrait(TraitWarmth) > 0.7f) return "温暖亲切的人";
            if (GetTrait(TraitCourage) > 0.7f) return "勇敢果断的人";
            if (GetTrait(TraitCompassion) > 0.7f) return "富有同情心的人";
            if (GetTrait(TraitOptimism) > 0.7f) return "乐观开朗的人";
            return "性格温和的人";
        }

        public static PersonalityEngine CreatePreset(string preset)
        {
            var e = new PersonalityEngine();
            switch (preset)
            {
                case "kind": e.FromDict(new Dictionary<string, float> { { TraitWarmth, 0.8f }, { TraitCompassion, 0.8f }, { TraitSelfishness, 0.2f } }); break;
                case "selfish": e.FromDict(new Dictionary<string, float> { { TraitWarmth, 0.3f }, { TraitCompassion, 0.2f }, { TraitSelfishness, 0.8f } }); break;
                case "brave": e.FromDict(new Dictionary<string, float> { { TraitCourage, 0.9f }, { TraitPersistence, 0.8f }, { TraitFearTendency, 0.1f } }); break;
                case "creative": e.FromDict(new Dictionary<string, float> { { TraitCreativity, 0.9f }, { TraitCuriosity, 0.8f }, { TraitOptimism, 0.7f } }); break;
                case "grumpy": e.FromDict(new Dictionary<string, float> { { TraitAngerTendency, 0.7f }, { TraitPatience, 0.2f }, { TraitOptimism, 0.3f } }); break;
            }
            return e;
        }
    }
}
