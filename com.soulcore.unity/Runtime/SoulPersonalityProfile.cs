using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulCore
{
    [CreateAssetMenu(menuName = "魂核/人格配置 Profile", fileName = "SoulPersonalityProfile")]
    public class SoulPersonalityProfile : ScriptableObject
    {
        [Header("基础风格")]
        [Range(0f, 1f)] public float warmth = 0.5f;
        [Range(0f, 1f)] public float compassion = 0.5f;
        [Range(0f, 1f)] public float trust = 0.5f;
        [Range(0f, 1f)] public float optimism = 0.5f;

        [Header("行为倾向")]
        [Range(0f, 1f)] public float courage = 0.5f;
        [Range(0f, 1f)] public float curiosity = 0.5f;
        [Range(0f, 1f)] public float selfishness = 0.5f;
        [Range(0f, 1f)] public float fearTendency = 0.2f;

        [Header("表达风格")]
        [Range(0f, 1f)] public float patience = 0.5f;
        [Range(0f, 1f)] public float rationality = 0.5f;
        [Range(0f, 1f)] public float creativity = 0.5f;
        [Range(0f, 1f)] public float persistence = 0.5f;

        public Dictionary<string, float> ToTraitOverrides()
        {
            return new Dictionary<string, float>
            {
                { PersonalityEngine.TraitWarmth, Clamp01(warmth) },
                { PersonalityEngine.TraitCompassion, Clamp01(compassion) },
                { PersonalityEngine.TraitTrust, Clamp01(trust) },
                { PersonalityEngine.TraitOptimism, Clamp01(optimism) },
                { PersonalityEngine.TraitCourage, Clamp01(courage) },
                { PersonalityEngine.TraitCuriosity, Clamp01(curiosity) },
                { PersonalityEngine.TraitSelfishness, Clamp01(selfishness) },
                { PersonalityEngine.TraitFearTendency, Clamp01(fearTendency) },
                { PersonalityEngine.TraitPatience, Clamp01(patience) },
                { PersonalityEngine.TraitRationality, Clamp01(rationality) },
                { PersonalityEngine.TraitCreativity, Clamp01(creativity) },
                { PersonalityEngine.TraitPersistence, Clamp01(persistence) }
            };
        }

        private static float Clamp01(float v) => Math.Clamp(v, 0f, 1f);
    }
}
