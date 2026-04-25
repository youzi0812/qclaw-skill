using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulCore.Advanced
{
    /// <summary>
    /// v1.2: 可波动动机模型，避免社交分值长期卡死。
    /// </summary>
    public sealed class MotivationModule : IAdvancedModule
    {
        private readonly Dictionary<int, float> _socialMomentum = new();
        private readonly float _baseSocialDrive;
        private readonly float _lonelinessWeight;
        private readonly float _missingWeight;
        private readonly float _relationWeight;
        private readonly float _driftSpeed;
        private readonly float _noiseAmplitude;

        public MotivationModule(
            float baseSocialDrive = 0.16f,
            float lonelinessWeight = 0.52f,
            float missingWeight = 0.18f,
            float relationWeight = 0.03f,
            float driftSpeed = 0.12f,
            float noiseAmplitude = 0.08f)
        {
            _baseSocialDrive = Math.Clamp(baseSocialDrive, 0f, 1f);
            _lonelinessWeight = Math.Clamp(lonelinessWeight, 0f, 2f);
            _missingWeight = Math.Clamp(missingWeight, 0f, 2f);
            _relationWeight = Math.Clamp(relationWeight, 0f, 2f);
            _driftSpeed = Math.Clamp(driftSpeed, 0.01f, 1.0f);
            _noiseAmplitude = Math.Clamp(noiseAmplitude, 0f, 0.35f);
        }

        public string ModuleId => "motivation.v1.2";

        public void Tick(AdvancedNpcState state, float dt)
        {
            if (state == null || state.Npc == null || state.Npc.Soul == null) return;
            var soul = state.Npc.Soul;
            var emo = soul.Emotion != null ? soul.Emotion.Emotions : null;
            var loneliness = _tryEmotion(emo, "loneliness");
            var missing = _tryEmotion(emo, "missing");
            var relationBias = _estimateRelationBias(state.Npc);
            var momentum = _updateMomentum(state.Npc, loneliness, missing, dt);

            var baseScore = _baseSocialDrive
                            + loneliness * _lonelinessWeight
                            + missing * _missingWeight
                            + relationBias * _relationWeight
                            + momentum;

            // 低频漂移（每个 NPC 相位不同）+ 轻噪声，避免长期固定分值。
            var time = (float)DateTime.UtcNow.TimeOfDay.TotalSeconds;
            var phase = Mathf.Abs(state.Npc.GetInstanceID() % 17) * 0.37f;
            var wave = Mathf.Sin((time + phase) * _driftSpeed) * _noiseAmplitude;
            var noise = _hashNoise01(time * 0.17f + phase) * _noiseAmplitude * 0.45f;
            var social = baseScore + wave + noise;
            var now = DateTime.Now;
            var hour = now.Hour + now.Minute / 60f;
            var npcPhase01 = Mathf.Abs(state.Npc.GetInstanceID() % 11) / 10f;
            var restBias = (npcPhase01 - 0.5f) * 0.08f;
            var workBias = (0.5f - npcPhase01) * 0.10f;
            var solitudeBias = Mathf.Sin((hour * 0.65f) + npcPhase01 * 3.1f) * 0.05f;

            var lateNight = _gauss(hour, 2f, 2.6f) * 0.52f + _gauss(hour, 23f, 1.8f) * 0.24f;
            var fatigue = _tryEmotion(emo, "fatigue");
            var sadness = _tryEmotion(emo, "sadness");
            var interactions = _dailyInteractions01(soul);
            var middayDip = _gauss(hour, 14f, 1.6f) * 0.18f; // 午后轻疲劳
            var rest = 0.14f + lateNight + middayDip + fatigue * 0.52f + sadness * 0.16f + restBias;

            var focus = _tryEmotion(emo, "focus");
            var memoryTotal = 0f;
            if (soul.Memory != null && soul.Memory.Stats != null && soul.Memory.Stats.TryGetValue("total", out var m))
            {
                memoryTotal = Mathf.Clamp01(m / 120f);
            }
            var workPrime = _gauss(hour, 10.5f, 3.2f) * 0.30f + _gauss(hour, 16.5f, 2.2f) * 0.10f; // 白天更易工作
            var work = 0.16f + workPrime + focus * 0.52f + memoryTotal * 0.22f - loneliness * 0.12f + workBias;

            var anxiety = _tryEmotion(emo, "anxiety");
            var fear = _tryEmotion(emo, "fear");
            var solitude = 0.14f + anxiety * 0.34f + fear * 0.30f + relationBias * -0.08f + solitudeBias;

            // 社交疲劳（调温版）：避免短时间内把全体 NPC 推入高 Rest 浪潮。
            social -= interactions * 0.20f;
            rest += interactions * 0.04f;
            solitude += interactions * 0.09f;

            state.Drives[AdvancedDriveKeys.Socialize] = Math.Clamp(social, 0f, 1f);
            state.Drives[AdvancedDriveKeys.Rest] = Math.Clamp(rest, 0f, 1f);
            state.Drives[AdvancedDriveKeys.Work] = Math.Clamp(work, 0f, 1f);
            state.Drives[AdvancedDriveKeys.Solitude] = Math.Clamp(solitude, 0f, 1f);
            state.Signals[AdvancedSignalKeys.Loneliness] = loneliness;
            state.Signals[AdvancedSignalKeys.Missing] = missing;
            state.Signals[AdvancedSignalKeys.SocialBase] = Math.Clamp(baseScore, 0f, 1f);
            state.Signals[AdvancedSignalKeys.SocialWave] = wave;
            state.Signals[AdvancedSignalKeys.SocialNoise] = noise;
            state.Signals[AdvancedSignalKeys.SocialMomentum] = momentum;
            state.Signals[AdvancedSignalKeys.ClockHour] = hour;
            state.Signals[AdvancedSignalKeys.LateNight] = lateNight;
            state.Signals[AdvancedSignalKeys.DailyInteractions01] = interactions;
        }

        private static float _tryEmotion(System.Collections.Generic.Dictionary<string, float> emo, string key)
        {
            if (emo == null || string.IsNullOrEmpty(key)) return 0f;
            return emo.TryGetValue(key, out var v) ? Math.Clamp(v, 0f, 1f) : 0f;
        }

        private static float _estimateRelationBias(SoulNPC npc)
        {
            if (npc == null || npc.Soul == null) return 0f;
            // 避免“聊得越多越想继续聊”的正反馈爆发：
            // v1.1 暂时不使用 daily_interactions 作为驱动项。
            // 未来改为“群体关系均值”或“目标关系差异”再回填。
            return 0f;
        }

        private float _updateMomentum(SoulNPC npc, float loneliness, float missing, float dt)
        {
            if (npc == null) return 0f;
            var key = npc.GetInstanceID();
            _socialMomentum.TryGetValue(key, out var m);

            var gain = (loneliness * 0.10f + missing * 0.05f) * Mathf.Max(0.01f, dt);
            var decay = 0.015f * Mathf.Max(0.01f, dt);
            m = Mathf.Clamp(m + gain - decay, 0f, 0.14f);
            _socialMomentum[key] = m;
            return m;
        }

        private static float _hashNoise01(float x)
        {
            // 轻量哈希噪声，输出约 [-0.5, 0.5]
            var v = Mathf.Sin(x * 12.9898f) * 43758.5453f;
            return (v - Mathf.Floor(v)) - 0.5f;
        }

        private static float _gauss(float x, float center, float sigma)
        {
            var d = x - center;
            return Mathf.Exp(-(d * d) / (2f * sigma * sigma));
        }

        private static float _dailyInteractions01(Soul soul)
        {
            if (soul == null || soul.Stats == null) return 0f;
            return soul.Stats.TryGetValue("daily_interactions", out var v) ? Mathf.Clamp01(v / 20f) : 0f;
        }
    }
}
