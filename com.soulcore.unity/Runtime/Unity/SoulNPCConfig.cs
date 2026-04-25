using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulCore
{
    [CreateAssetMenu(menuName = "魂核/Soul NPC 配置", fileName = "SoulNPCConfig")]
    public class SoulNPCConfig : ScriptableObject
    {
        [Serializable]
        public class InitialRelationEntry
        {
            public string targetNpcId;
            [Range(-10, 10)] public int relationValue;
        }

        [Header("极简配置")]
        [Tooltip("可选：当 SoulNPC 的「显示名称」为空时，会优先采用这里的名称。")]
        public string suggestedDisplayName = "";

        [Tooltip("可选：一句话人格描述，用于自动推断 personalityPreset（不改高级滑条）。")]
        [TextArea(1, 2)] public string oneLinePersonality = "";

        [Tooltip("可选：初始化时写入关系值（范围 -10~10）。")]
        public List<InitialRelationEntry> initialRelations = new();

        [Header("人格")]
        [Tooltip("default / kind / brave / selfish / creative / grumpy 等，见 PersonalityEngine.CreatePreset")]
        public string personalityPreset = "default";
        [Tooltip("可选：指定人格 Profile 资产；若指定，则会在预设基础上覆盖对应人格特质。")]
        public SoulPersonalityProfile personalityProfile;
        [Tooltip("当 preset=default 且未绑定 Profile / 内联覆盖时，按 NPC ID 对关键 trait 做小幅可复现离散，避免所有 NPC 人格完全一致。")]
        public bool autoDiversifyDefaultPersonality = true;
        [Range(0f, 0.35f), Tooltip("默认人格离散强度。建议 0.06~0.15；0 表示关闭扰动。")]
        public float defaultPersonalityDiversifyStrength = 0.10f;
        [Tooltip("未绑定人格资产时，是否启用内联人格个性化覆盖（由人格细化面板写入）。")]
        public bool inlinePersonalityEnabled;
        [Range(0f, 1f)] public float inlineWarmth = 0.5f;
        [Range(0f, 1f)] public float inlineCompassion = 0.5f;
        [Range(0f, 1f)] public float inlineTrust = 0.5f;
        [Range(0f, 1f)] public float inlineOptimism = 0.5f;
        [Range(0f, 1f)] public float inlineCourage = 0.5f;
        [Range(0f, 1f)] public float inlineCuriosity = 0.5f;
        [Range(0f, 1f)] public float inlineSelfishness = 0.5f;
        [Range(0f, 1f)] public float inlineFearTendency = 0.2f;
        [Range(0f, 1f)] public float inlinePatience = 0.5f;
        [Range(0f, 1f)] public float inlineRationality = 0.5f;
        [Range(0f, 1f)] public float inlineCreativity = 0.5f;
        [Range(0f, 1f)] public float inlinePersistence = 0.5f;

        [Header("记忆与情感")]
        [Min(8)] public int maxMemories = 200;
        [Min(0.1f)] public float emotionDailyDecayScale = 1f;

        [Header("人性化辅文案 (不影响主决策)")]
        [Tooltip("套用：由引擎用轻量模板填充玩笑/槽位等；自设：不生成，SoulDecision 对应字段为 null，由你方填充。")]
        public SoulFlavorOutputMode flavorOutput = SoulFlavorOutputMode.BuiltInTemplates;

        [Header("决策引擎权重 (合计不必为 1，会相对生效)")]
        [Range(0.01f, 1f)] public float decisionWeightPersonality = 0.25f;
        [Range(0.01f, 1f)] public float decisionWeightEmotion = 0.25f;
        [Range(0.01f, 1f)] public float decisionWeightMemory = 0.20f;
        [Range(0.01f, 1f)] public float decisionWeightResource = 0.30f;

        [Header("模块开关 (关闭以省算力)")]
        public bool moduleEmotion = true;
        public bool moduleMemory = true;
        public bool moduleDecision = true;
        public bool moduleRelationship = true;
        public bool moduleInterest = true;
        public bool moduleCuriosity = true;
        public bool moduleHabit = true;
        public bool moduleSerendipity = true;
        public bool moduleEmotionInfect = true;
        public bool moduleHumor = true;
        public bool moduleComplain = true;
        public bool moduleEmbarrassment = true;
        public bool moduleExploration = true;
        public bool modulePreference = true;
        public bool moduleNostalgia = true;
        public bool moduleAwe = true;
        public bool moduleIntuition = true;
        public bool moduleInspiration = true;
        public bool moduleDream = true;
        public bool moduleConsistency = true;

        public SoulModuleFlags GetModuleFlags() => new SoulModuleFlags
        {
            Emotion = moduleEmotion,
            Memory = moduleMemory,
            Decision = moduleDecision,
            Relationship = moduleRelationship,
            Interest = moduleInterest,
            Curiosity = moduleCuriosity,
            Habit = moduleHabit,
            Serendipity = moduleSerendipity,
            EmotionInfect = moduleEmotionInfect,
            Humor = moduleHumor,
            Complain = moduleComplain,
            Embarrassment = moduleEmbarrassment,
            Exploration = moduleExploration,
            Preference = modulePreference,
            Nostalgia = moduleNostalgia,
            Awe = moduleAwe,
            Intuition = moduleIntuition,
            Inspiration = moduleInspiration,
            Dream = moduleDream,
            Consistency = moduleConsistency
        };

        public SoulRuntimeParams ToRuntimeParams()
        {
            var preset = string.IsNullOrEmpty(personalityPreset) ? "default" : personalityPreset;
            if ((preset == "default" || string.IsNullOrEmpty(personalityPreset)) && !string.IsNullOrEmpty(oneLinePersonality))
                preset = InferPresetFromOneLine(oneLinePersonality);
            var p = new SoulRuntimeParams
            {
                PersonalityPreset = preset,
                MaxMemories = maxMemories,
                EmotionDailyDecayScale = emotionDailyDecayScale,
                DecisionPersonalityWeight = decisionWeightPersonality,
                DecisionEmotionWeight = decisionWeightEmotion,
                DecisionMemoryWeight = decisionWeightMemory,
                DecisionResourceWeight = decisionWeightResource,
                FlavorOutput = flavorOutput,
                AutoDiversifyDefaultPersonality = autoDiversifyDefaultPersonality,
                AutoDiversifyStrength = defaultPersonalityDiversifyStrength
            };
            if (personalityProfile != null)
            {
                p.PersonalityOverrides = personalityProfile.ToTraitOverrides();
            }
            else if (inlinePersonalityEnabled)
            {
                p.PersonalityOverrides = _inlineOverrides();
            }
            p.Modules = GetModuleFlags();
            return p;
        }

        public void ApplyInlinePersonalityFromProfile(SoulPersonalityProfile profile)
        {
            if (profile == null) return;
            inlinePersonalityEnabled = true;
            inlineWarmth = profile.warmth;
            inlineCompassion = profile.compassion;
            inlineTrust = profile.trust;
            inlineOptimism = profile.optimism;
            inlineCourage = profile.courage;
            inlineCuriosity = profile.curiosity;
            inlineSelfishness = profile.selfishness;
            inlineFearTendency = profile.fearTendency;
            inlinePatience = profile.patience;
            inlineRationality = profile.rationality;
            inlineCreativity = profile.creativity;
            inlinePersistence = profile.persistence;
        }

        public void FillProfileFromInline(SoulPersonalityProfile profile)
        {
            if (profile == null) return;
            profile.warmth = inlineWarmth;
            profile.compassion = inlineCompassion;
            profile.trust = inlineTrust;
            profile.optimism = inlineOptimism;
            profile.courage = inlineCourage;
            profile.curiosity = inlineCuriosity;
            profile.selfishness = inlineSelfishness;
            profile.fearTendency = inlineFearTendency;
            profile.patience = inlinePatience;
            profile.rationality = inlineRationality;
            profile.creativity = inlineCreativity;
            profile.persistence = inlinePersistence;
        }

        private Dictionary<string, float> _inlineOverrides()
        {
            return new Dictionary<string, float>
            {
                { PersonalityEngine.TraitWarmth, inlineWarmth },
                { PersonalityEngine.TraitCompassion, inlineCompassion },
                { PersonalityEngine.TraitTrust, inlineTrust },
                { PersonalityEngine.TraitOptimism, inlineOptimism },
                { PersonalityEngine.TraitCourage, inlineCourage },
                { PersonalityEngine.TraitCuriosity, inlineCuriosity },
                { PersonalityEngine.TraitSelfishness, inlineSelfishness },
                { PersonalityEngine.TraitFearTendency, inlineFearTendency },
                { PersonalityEngine.TraitPatience, inlinePatience },
                { PersonalityEngine.TraitRationality, inlineRationality },
                { PersonalityEngine.TraitCreativity, inlineCreativity },
                { PersonalityEngine.TraitPersistence, inlinePersistence }
            };
        }

        public void ApplyInitialRelationships(Soul soul)
        {
            if (soul == null || initialRelations == null || initialRelations.Count == 0) return;
            foreach (var r in initialRelations)
            {
                if (r == null || string.IsNullOrWhiteSpace(r.targetNpcId)) continue;
                var rel = soul.Relationship.Get(soul.Id, r.targetNpcId.Trim());
                rel.Value = Math.Clamp(r.relationValue, -10, 10);
            }
        }

        public static string InferPresetFromOneLine(string sentence)
        {
            if (string.IsNullOrEmpty(sentence)) return "default";
            var s = sentence.ToLowerInvariant();
            if (s.Contains("自私") || s.Contains("selfish")) return "selfish";
            if (s.Contains("勇敢") || s.Contains("brave")) return "brave";
            if (s.Contains("创造") || s.Contains("creative")) return "creative";
            if (s.Contains("暴躁") || s.Contains("grumpy")) return "grumpy";
            if (s.Contains("善良") || s.Contains("温和") || s.Contains("kind")) return "kind";
            return "default";
        }
    }
}
