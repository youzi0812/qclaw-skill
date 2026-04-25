using System.Collections.Generic;
using UnityEngine;

namespace SoulCore.Advanced
{
    /// <summary>
    /// 6.1.8 — 叙事自我 v0.1：从情感与记忆体量生成「叙事连贯度 / 张力 / 立场」标量，写入 Signals。
    /// </summary>
    public sealed class NarrativeSelfModule : IAdvancedModule
    {
        public string ModuleId => "narrative_self.v0.1";

        public void Tick(AdvancedNpcState state, float dt)
        {
            if (state == null || state.Npc == null || state.Npc.Soul == null) return;
            var soul = state.Npc.Soul;
            var emo = soul.Emotion != null ? soul.Emotion.Emotions : null;

            var joy = _emo(emo, "joy");
            var sadness = _emo(emo, "sadness");
            var loneliness = _emo(emo, "loneliness");
            var missing = _emo(emo, "missing");
            var anxiety = _emo(emo, "anxiety");
            var fear = _emo(emo, "fear");

            var memoryTotal = 0f;
            if (soul.Memory != null && soul.Memory.Stats != null && soul.Memory.Stats.TryGetValue("total", out var m))
            {
                memoryTotal = Mathf.Clamp01(m / 120f);
            }

            // 叙事连贯度：情绪不过度撕裂 + 记忆适中（太少飘、太多乱）
            var emotionalSpread = Mathf.Abs(joy - sadness) + Mathf.Abs(anxiety - fear) * 0.5f;
            var memorySweet = 1f - Mathf.Abs(memoryTotal - 0.45f) * 1.6f;
            memorySweet = Mathf.Clamp01(memorySweet);
            var coherence = Mathf.Clamp01(0.55f + joy * 0.15f - emotionalSpread * 0.22f + memorySweet * 0.25f - anxiety * 0.12f);

            // 叙事张力：冲突感
            var tension = Mathf.Clamp01(anxiety * 0.38f + fear * 0.32f + sadness * 0.22f + loneliness * 0.18f);

            // 立场 0..1：偏「外向叙事实践」vs「内向回溯」——用孤独/思念差粗略编码，非心理学严格定义
            var stance = Mathf.Clamp01(0.5f + (loneliness - missing) * 0.35f + (joy - sadness) * 0.15f);

            state.Signals[AdvancedSignalKeys.NarrativeCoherence] = coherence;
            state.Signals[AdvancedSignalKeys.NarrativeTension] = tension;
            state.Signals[AdvancedSignalKeys.NarrativeStance] = stance;
            state.Signals[AdvancedSignalKeys.NarrativeMemory01] = memoryTotal;
        }

        private static float _emo(Dictionary<string, float> emo, string key)
        {
            if (emo == null || string.IsNullOrEmpty(key)) return 0f;
            return emo.TryGetValue(key, out var v) ? Mathf.Clamp01(v) : 0f;
        }
    }
}
