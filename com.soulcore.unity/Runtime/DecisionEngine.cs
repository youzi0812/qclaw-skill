using System;
using System.Collections.Generic;
using System.Linq;

namespace SoulCore
{
    public class DecisionEngine
    {
        private readonly ISoulState _soul;
        private readonly System.Random _rng;

        public readonly Dictionary<string, float> Weights = new()
        {
            { "personality", 0.25f },
            { "emotion", 0.25f },
            { "memory", 0.20f },
            { "resource", 0.30f }
        };

        public readonly List<Dictionary<string, object>> History = new();

        private static readonly Dictionary<string, (float[] th, string[] act)> Mappings = new()
        {
            { "help", (new[] { 0.6f }, new[] { "help_others", "self_first" }) },
            { "conversation", (new[] { 0.7f, 0.4f }, new[] { "open_up", "listen", "deflect", "silence" }) },
            { "evacuation", (new[] { 0.7f, 0.5f, 0.3f }, new[] { "leave_now", "prepare", "wait", "stay" }) },
            { "rescue", (new[] { 0.7f, 0.5f }, new[] { "go_rescue", "call_help", "stay_safe" }) },
            { "learn", (new[] { 0.5f }, new[] { "learn", "ignore" }) },
            { "default", (new[] { 0.6f }, new[] { "proceed", "wait", "avoid" }) }
        };

        public DecisionEngine(ISoulState soul, int randomSeed = 0)
        {
            _soul = soul;
            _rng = randomSeed == 0 ? new System.Random() : new System.Random(randomSeed);
        }

        public (string action, string explanation, float confidence) Decide(
            string sitType,
            IReadOnlyDictionary<string, string> situation,
            List<Memory> memories)
        {
            if (!Mappings.TryGetValue(sitType, out var map)) map = Mappings["default"];
            var personalityScore = _scorePersonality(sitType);
            var emotionScore = _scoreEmotion();
            var memoryScore = _scoreMemory(memories);
            var resourceScore = _scoreResource(sitType);
            var final = personalityScore * Weights["personality"]
                      + emotionScore * Weights["emotion"]
                      + memoryScore * Weights["memory"]
                      + resourceScore * Weights["resource"];
            final += (float)(_rng.NextDouble() * 0.2 - 0.1);
            final = Math.Clamp(final, 0f, 1f);

            var (action, confidence) = _mapToAction(final, map);
            var explanation = _generateExplanation(action, final, sitType, situation, memories);
            return (action, explanation, confidence);
        }

        private (string, float) _mapToAction(float score, (float[] th, string[] act) map)
        {
            for (var i = 0; i < map.th.Length; i++)
            {
                if (score >= map.th[i])
                    return (map.act[i], score);
            }
            return (map.act[map.act.Length - 1], score);
        }

        private float _scorePersonality(string sit)
        {
            var t = _soul.Personality.Traits;
            var score = 0.5f;
            switch (sit)
            {
                case "help":
                    if (Get(t, "compassion", 0.5f) > 0.6f) score += 0.2f;
                    if (Get(t, "selfishness", 0.5f) > 0.6f) score -= 0.2f;
                    break;
                case "conversation":
                    if (Get(t, "warmth", 0.5f) > 0.6f) score += 0.2f;
                    if (Get(t, "trust", 0.5f) > 0.6f) score += 0.1f;
                    if (Get(t, "curiosity", 0.5f) > 0.6f) score += 0.08f;
                    if (Get(t, "selfishness", 0.5f) > 0.62f) score -= 0.12f;
                    if (Get(t, "fear_tendency", 0.2f) > 0.65f) score -= 0.08f;
                    break;
                case "evacuation":
                    if (Get(t, "courage", 0.5f) > 0.6f) score += 0.1f;
                    if (Get(t, "fear_tendency", 0.2f) > 0.6f) score -= 0.1f;
                    if (Get(t, "persistence", 0.5f) > 0.7f) score += 0.1f;
                    break;
                case "rescue":
                    if (Get(t, "courage", 0.5f) > 0.7f) score += 0.2f;
                    if (Get(t, "compassion", 0.5f) > 0.6f) score += 0.1f;
                    if (Get(t, "selfishness", 0.5f) > 0.7f) score -= 0.2f;
                    break;
                case "learn":
                    if (Get(t, "curiosity", 0.5f) > 0.6f) score += 0.2f;
                    if (Get(t, "creativity", 0.5f) > 0.6f) score += 0.1f;
                    break;
            }
            return Math.Clamp(score, 0f, 1f);
        }

        private static float Get(Dictionary<string, float> d, string k, float def) =>
            d.TryGetValue(k, out var v) ? v : def;

        private float _scoreEmotion()
        {
            var e = _soul.Emotion.Emotions;
            var score = 0.5f;
            if (GetD(e, "fear", 0) > 0.7f) score -= 0.2f;
            if (GetD(e, "hope", 0) > 0.7f) score += 0.2f;
            if (GetD(e, "joy", 0) > 0.7f) score += 0.1f;
            if (GetD(e, "despair", 0) > 0.8f) score -= 0.3f;
            if (GetD(e, "curiosity_emotion", 0) > 0.7f) score += 0.1f;
            return Math.Clamp(score, 0f, 1f);
        }

        private static float GetD(Dictionary<string, float> d, string k, float def) =>
            d.TryGetValue(k, out var v) ? v : def;

        private static float _scoreMemory(List<Memory> memories)
        {
            if (memories == null || memories.Count == 0) return 0.5f;
            var score = 0.5f;
            foreach (var mem in memories.Take(3))
            {
                if (string.IsNullOrEmpty(mem?.Content)) continue;
                if (mem.Content.Contains("成功") || mem.Content.Contains("帮助") || mem.Content.Contains("学会"))
                    score += 0.1f;
                else if (mem.Content.Contains("失败") || mem.Content.Contains("背叛") || mem.Content.Contains("错误"))
                    score -= 0.1f;
            }
            return Math.Clamp(score, 0f, 1f);
        }

        private float _scoreResource(string sitType)
        {
            var s = _soul.GetResourceScarcity();
            return sitType switch
            {
                "help" => 0.7f - s * 0.5f,
                "rescue" => 0.8f - s * 0.6f,
                _ => 0.5f - s * 0.2f
            };
        }

        private string _generateExplanation(
            string action,
            float score,
            string sitType,
            IReadOnlyDictionary<string, string> situation,
            List<Memory> memories)
        {
            var name = _soul.Name;
            var traits = _soul.Personality.Traits;
            var emo = _soul.Emotion.Emotions;
            var reasons = new List<string>();
            var content = _getSituationContent(situation);
            var memoryHint = _getMemoryHint(memories);
            if (Get(traits, "compassion", 0.5f) > 0.7f) reasons.Add("我看不得别人受苦");
            if (Get(traits, "selfishness", 0.5f) > 0.7f) reasons.Add("我得先顾自己");
            if (Get(traits, "courage", 0.5f) > 0.7f) reasons.Add("我不怕");
            if (Get(traits, "curiosity", 0.5f) > 0.7f) reasons.Add("我很好奇");
            if (GetD(emo, "hope", 0) > 0.7f) reasons.Add("我觉得还有希望");
            if (GetD(emo, "fear", 0) > 0.7f) reasons.Add("我有点害怕");
            if (GetD(emo, "curiosity_emotion", 0) > 0.7f) reasons.Add("我很好奇");
            if (_soul.GetResourceScarcity() > 0.5f) reasons.Add("资源有些紧张");
            if (sitType == "conversation" && !string.IsNullOrEmpty(content))
                reasons.Add($"你提到{content}");
            if (!string.IsNullOrEmpty(memoryHint))
                reasons.Add($"我想起{memoryHint}");
            if (sitType == "conversation" && reasons.Count == 0)
                reasons.Add("我先顺着你的话回应");
            if (reasons.Count == 0) reasons.Add("我也说不清为什么");

            var actionDesc = action switch
            {
                "help_others" => "帮一把", "self_first" => "先顾自己",
                "open_up" => "敞开心扉", "listen" => "听你说", "deflect" => "不太想说", "silence" => "沉默",
                "leave_now" => "马上走", "prepare" => "准备一下", "wait" => "等等看", "stay" => "不走了",
                "go_rescue" => "去救人", "call_help" => "求助", "stay_safe" => "保护好自己",
                "learn" => "学一下", "ignore" => "先不管",
                "proceed" => "继续", "avoid" => "避开", _ => action
            };
            var reasonText = string.Join("、", reasons.Take(2));
            var templateIndex = _rng.Next(0, 3);
            var core = templateIndex switch
            {
                0 => $"{name}决定{actionDesc}，因为{reasonText}",
                1 => $"{name}选择{actionDesc}，因为{reasonText}",
                _ => $"{name}这次会{actionDesc}，主要是{reasonText}"
            };
            if (sitType == "conversation")
            {
                return core + "。" + _conversationStyleLine(name, traits, emo);
            }
            return core;
        }

        private static string _getSituationContent(IReadOnlyDictionary<string, string> situation)
        {
            if (situation == null) return null;
            if (!situation.TryGetValue("content", out var content) || string.IsNullOrWhiteSpace(content)) return null;
            content = content.Trim();
            return content.Length <= 14 ? content : content.Substring(0, 14) + "…";
        }

        private static string _getMemoryHint(List<Memory> memories)
        {
            if (memories == null || memories.Count == 0) return null;
            for (var i = 0; i < memories.Count; i++)
            {
                var c = memories[i]?.Content;
                if (string.IsNullOrWhiteSpace(c)) continue;
                c = c.Trim();
                if (_isGenericMemoryNoise(c)) continue;
                return c.Length <= 14 ? c : c.Substring(0, 14) + "…";
            }
            return null;
        }

        private static bool _isGenericMemoryNoise(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return true;
            // 过滤演示中观感较差、信息价值较低的系统模板记忆
            return content.Contains("事件，强度")
                   || content.Contains("出生")
                   || content.Contains("梦见")
                   || content.Contains("rt.soul.");
        }

        private static string _conversationStyleLine(string name, Dictionary<string, float> traits, Dictionary<string, float> emo)
        {
            if (Get(traits, "compassion", 0.5f) > 0.7f) return "我会先照顾你的感受。";
            if (Get(traits, "selfishness", 0.5f) > 0.7f) return "我会先确认这件事对我的影响。";
            if (Get(traits, "curiosity", 0.5f) > 0.72f || GetD(emo, "curiosity_emotion", 0f) > 0.7f) return "我想再追问一两个细节。";
            if (GetD(emo, "fear", 0f) > 0.72f || Get(traits, "fear_tendency", 0.2f) > 0.72f) return "我会先保守一点，慢慢推进。";

            // 当特质差异不明显时，给不同 NPC 固定一条“说话风格尾句”，避免演示里看起来完全一样。
            var idx = Math.Abs((name ?? string.Empty).GetHashCode()) % 3;
            return idx switch
            {
                0 => "我们一步一步来。",
                1 => "先把重点捋清楚。",
                _ => "你继续说，我在听。"
            };
        }
    }
}
