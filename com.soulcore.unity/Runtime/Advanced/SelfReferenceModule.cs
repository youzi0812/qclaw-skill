using System.Collections.Generic;
using UnityEngine;

namespace SoulCore.Advanced
{
    /// <summary>
    /// 6.1.8 — 自指涉 v0.1：极轻量的「自我连贯 / 元认知负荷」标量，写入 Signals；避免与叙事自我重复叙事文本。
    /// </summary>
    public sealed class SelfReferenceModule : IAdvancedModule
    {
        public string ModuleId => "self_reference.v0.1";

        public void Tick(AdvancedNpcState state, float dt)
        {
            if (state == null || state.Npc == null || state.Npc.Soul == null) return;
            var soul = state.Npc.Soul;
            var emo = soul.Emotion != null ? soul.Emotion.Emotions : null;

            var anxiety = _emo(emo, "anxiety");
            var fatigue = _emo(emo, "fatigue");
            var focus = _emo(emo, "focus");

            var coherenceSignal = state.Signals.TryGetValue(AdvancedSignalKeys.NarrativeCoherence, out var nc) ? Mathf.Clamp01(nc) : 0.5f;
            var clarity = state.Signals.TryGetValue(AdvancedSignalKeys.ConsciousnessClarity, out var cl) ? Mathf.Clamp01(cl) : 0.5f;
            var fePressure = state.Signals.TryGetValue(AdvancedSignalKeys.FePressure, out var fe) ? Mathf.Clamp01(fe) : 0f;

            // 自我连贯：叙事连贯与意识清晰度的融合，自由能压力略削弱
            var selfContinuity = Mathf.Clamp01(coherenceSignal * 0.45f + clarity * 0.4f + focus * 0.2f - fePressure * 0.18f - anxiety * 0.12f);

            // 元认知负荷：焦虑 × 疲劳，高时「关于自身的加工」成本上升
            var metaLoad = Mathf.Clamp01(anxiety * 0.45f + fatigue * 0.35f + fePressure * 0.25f - focus * 0.1f);

            // 自指张力：内在状态与「可叙述自我」的摩擦（v0.1 启发式）
            var reflexiveTension = Mathf.Clamp01(metaLoad * 0.55f + (1f - selfContinuity) * 0.35f);

            state.Signals[AdvancedSignalKeys.SelfContinuity] = selfContinuity;
            state.Signals[AdvancedSignalKeys.SelfMetaLoad] = metaLoad;
            state.Signals[AdvancedSignalKeys.SelfReflexiveTension] = reflexiveTension;
        }

        private static float _emo(Dictionary<string, float> emo, string key)
        {
            if (emo == null || string.IsNullOrEmpty(key)) return 0f;
            return emo.TryGetValue(key, out var v) ? Mathf.Clamp01(v) : 0f;
        }
    }
}
