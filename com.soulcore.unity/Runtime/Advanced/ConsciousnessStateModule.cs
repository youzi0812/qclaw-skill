using System.Collections.Generic;
using UnityEngine;

namespace SoulCore.Advanced
{
    /// <summary>
    /// 6.1.8 — 意识状态 v0.1：从情感与当前意图推导「警觉度 / 清晰度 / 负荷」标量，供后续策略或其它模块读取。
    /// 不替代 <see cref="MotivationModule"/>，仅写 Signals。
    /// </summary>
    public sealed class ConsciousnessStateModule : IAdvancedModule
    {
        public string ModuleId => "consciousness_state.v0.1";

        public void Tick(AdvancedNpcState state, float dt)
        {
            if (state == null || state.Npc == null || state.Npc.Soul == null) return;
            var soul = state.Npc.Soul;
            var emo = soul.Emotion != null ? soul.Emotion.Emotions : null;

            var fatigue = _emo(emo, "fatigue");
            var focus = _emo(emo, "focus");
            var anxiety = _emo(emo, "anxiety");
            var joy = _emo(emo, "joy");

            // 警觉度：疲劳压低、专注与轻度焦虑抬高（清醒但未必舒适）
            var arousal = Mathf.Clamp01(0.42f - fatigue * 0.38f + focus * 0.32f + anxiety * 0.22f + joy * 0.08f);

            // 清晰度：专注主导，焦虑模糊化
            var clarity = Mathf.Clamp01(focus * (1f - anxiety * 0.55f));

            // 负荷：疲劳 + 焦虑 + 反专注
            var load = Mathf.Clamp01(fatigue * 0.5f + anxiety * 0.35f + (1f - focus) * 0.2f);

            // 漂移倾向（类 DMN/走神）：高负荷低清晰时升高，不单独做 DMN 模块
            var drift = Mathf.Clamp01(load * 0.55f + (1f - clarity) * 0.35f - focus * 0.15f);

            // 与上一意图的衔接：刚执行高强度非社交后略增负荷感（单帧衰减由 load 已吸收，此处只做轻标记）
            var intentHangover = state.Intent.Kind is AdvancedIntentKind.Rest or AdvancedIntentKind.Solitude
                ? 0.06f
                : state.Intent.Kind == AdvancedIntentKind.Work
                    ? 0.04f
                    : 0f;
            load = Mathf.Clamp01(load + intentHangover);

            state.Signals[AdvancedSignalKeys.ConsciousnessArousal] = arousal;
            state.Signals[AdvancedSignalKeys.ConsciousnessClarity] = clarity;
            state.Signals[AdvancedSignalKeys.ConsciousnessLoad] = load;
            state.Signals[AdvancedSignalKeys.ConsciousnessDrift] = drift;
        }

        private static float _emo(Dictionary<string, float> emo, string key)
        {
            if (emo == null || string.IsNullOrEmpty(key)) return 0f;
            return emo.TryGetValue(key, out var v) ? Mathf.Clamp01(v) : 0f;
        }
    }
}
