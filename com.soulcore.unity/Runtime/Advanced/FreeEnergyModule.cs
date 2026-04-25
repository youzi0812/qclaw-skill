using System.Collections.Generic;
using UnityEngine;

namespace SoulCore.Advanced
{
    /// <summary>
    /// 6.1.8 — 自由能引擎 v0.1：用「社交基线突变 + 不确定情感」近似预测误差，写入 Signals（非全贝叶斯）。
    /// </summary>
    public sealed class FreeEnergyModule : IAdvancedModule
    {
        private readonly Dictionary<int, float> _lastSocialBase = new();

        public string ModuleId => "free_energy.v0.1";

        public void Tick(AdvancedNpcState state, float dt)
        {
            if (state == null || state.Npc == null || state.Npc.Soul == null) return;
            var soul = state.Npc.Soul;
            var emo = soul.Emotion != null ? soul.Emotion.Emotions : null;

            var anxiety = _emo(emo, "anxiety");
            var fear = _emo(emo, "fear");
            var surprise = _emo(emo, "surprise");

            // 动机模块每帧会写 social_base；用其一阶差分近似「预期违背」
            var socialBase = state.Signals.TryGetValue(AdvancedSignalKeys.SocialBase, out var sb) ? Mathf.Clamp01(sb) : 0f;
            var id = state.Npc.GetInstanceID();
            _lastSocialBase.TryGetValue(id, out var prev);
            var delta = Mathf.Abs(socialBase - prev);
            _lastSocialBase[id] = socialBase;
            var predictionError = Mathf.Clamp01(delta * 6f + surprise * 0.35f);

            // 不确定度：高焦虑低确定感
            var uncertainty = Mathf.Clamp01(anxiety * 0.55f + fear * 0.35f + (1f - Mathf.Abs(socialBase - 0.5f) * 0.2f) * 0.1f);

            // 简化的「自由能压力」：误差与不确定的凸组合
            var freeEnergyPressure = Mathf.Clamp01(predictionError * 0.55f + uncertainty * 0.45f);

            state.Signals[AdvancedSignalKeys.FePredictionError] = predictionError;
            state.Signals[AdvancedSignalKeys.FeUncertainty] = uncertainty;
            state.Signals[AdvancedSignalKeys.FePressure] = freeEnergyPressure;
        }

        private static float _emo(Dictionary<string, float> emo, string key)
        {
            if (emo == null || string.IsNullOrEmpty(key)) return 0f;
            return emo.TryGetValue(key, out var v) ? Mathf.Clamp01(v) : 0f;
        }
    }
}
