using System;
using System.Collections.Generic;

namespace SoulCore.Advanced
{
    /// <summary>
    /// v1: 9级优先级的最小实现骨架，当前输出社交意图。
    /// </summary>
    public sealed class PriorityPolicy9
    {
        /// <summary>自由能压力 → 略抬独处（高预测误差/不确定时更倾向收束）。</summary>
        private const float FePressureSolitudeGain = 0.055f;

        /// <summary>意识负荷 → 略抬休息（与 Motivation 的 rest 正交、量级很小）。</summary>
        private const float ConsciousnessLoadRestGain = 0.045f;
        /// <summary>人格社交趋近性 → 略抬社交。</summary>
        private const float PersonalitySocialGain = 0.025f;
        /// <summary>人格任务聚焦 → 略抬工作。</summary>
        private const float PersonalityWorkGain = 0.020f;
        /// <summary>人格情绪反应性 → 略抬独处并微抬休息。</summary>
        private const float PersonalityReactivitySolitudeGain = 0.020f;
        private const float PersonalityReactivityRestGain = 0.010f;
        /// <summary>社交进入近并列/偏好分支的默认绝对地板（可被编排器参数覆盖）。</summary>
        private const float DefaultSocialActivationFloor = 0.42f;

        public AdvancedIntent Evaluate(
            AdvancedNpcState state,
            float socializeThreshold,
            float restThreshold,
            float workThreshold,
            float solitudeThreshold,
            float socialActivationFloor = DefaultSocialActivationFloor)
        {
            if (state == null || state.Npc == null)
            {
                return new AdvancedIntent { Kind = AdvancedIntentKind.None, Score = 0f, Reason = "invalid_state" };
            }

            var social = _get(state, AdvancedDriveKeys.Socialize);
            var rest = _get(state, AdvancedDriveKeys.Rest);
            var work = _get(state, AdvancedDriveKeys.Work);
            var solitude = _get(state, AdvancedDriveKeys.Solitude);

            // 6.1.8 第二迭代：读 1～2 路已写入的 Signals，对进入优先级判定前的驱动做极轻修正（不改变 Motivation 公式）。
            _applySignalDriveShim(state, ref social, ref rest, ref work, ref solitude);

            // 供并列判定使用：重复惩罚会压低 social，若仅用惩罚后分值会与 Inspector 四驱动日志不一致，
            // 且易出现「原始 social 略高于 rest、惩罚后落入 fallback 随机成 Rest」的假阳性。
            var socialBeforePenalty = social;
            var restBeforePenalty = rest;
            _applyRepeatPenalty(state, ref social, ref rest, ref work, ref solitude);

            // 将「本帧实际参与优先级判定」的四值写回 Drives，使 TryGetDriveValues / 驱动诊断与 intent 一致。
            if (state.Drives != null)
            {
                state.Drives[AdvancedDriveKeys.Socialize] = Math.Clamp(social, 0f, 1f);
                state.Drives[AdvancedDriveKeys.Rest] = Math.Clamp(rest, 0f, 1f);
                state.Drives[AdvancedDriveKeys.Work] = Math.Clamp(work, 0f, 1f);
                state.Drives[AdvancedDriveKeys.Solitude] = Math.Clamp(solitude, 0f, 1f);
            }

            var tSocial = Math.Clamp(socializeThreshold, 0f, 1f);
            var tRest = Math.Clamp(restThreshold, 0f, 1f);
            var tWork = Math.Clamp(workThreshold, 0f, 1f);
            var tSolitude = Math.Clamp(solitudeThreshold, 0f, 1f);
            var socialFloor = Math.Clamp(socialActivationFloor, 0f, 1f);

            // 9级优先级（v2.3）：
            // 当社交只“略高一点”时，不立即压过其它意图，避免长期只出 Socialize。
            const float socialLeadMargin = 0.05f;
            var nonSocialMax = Math.Max(rest, Math.Max(work, solitude));
            var interactions01 = _getSignal(state, AdvancedSignalKeys.DailyInteractions01);
            var socialOverloaded = interactions01 >= 0.70f;
            var effectiveSocial = socialOverloaded ? Math.Max(0f, social - 0.04f) : social;
            var strongSocialLead = effectiveSocial >= (rest + 0.06f)
                                   && effectiveSocial >= (work + 0.06f)
                                   && effectiveSocial >= (solitude + 0.06f);

            // 兜底保护：社交明显领先时，不要被 Rest 分支“截胡”。
            if (strongSocialLead && effectiveSocial >= tSocial * 0.92f)
            {
                return new AdvancedIntent
                {
                    Kind = AdvancedIntentKind.Socialize,
                    Score = effectiveSocial,
                    Reason = socialOverloaded ? "priority.socialize.lead_guard.overloaded_soft" : "priority.socialize.lead_guard"
                };
            }

            // 先安全/恢复，再生产，再社交（仅当社交明显领先时）。
            if (rest >= tRest && rest >= solitude + 0.04f && rest >= effectiveSocial + 0.04f)
            {
                return new AdvancedIntent
                {
                    Kind = AdvancedIntentKind.Rest,
                    Score = rest,
                    Reason = "priority.rest"
                };
            }

            if (solitude >= tSolitude && solitude >= effectiveSocial + 0.02f)
            {
                return new AdvancedIntent
                {
                    Kind = AdvancedIntentKind.Solitude,
                    Score = solitude,
                    Reason = "priority.solitude"
                };
            }

            if (work >= tWork && work >= effectiveSocial + 0.02f)
            {
                return new AdvancedIntent
                {
                    Kind = AdvancedIntentKind.Work,
                    Score = work,
                    Reason = "priority.work"
                };
            }

            if (effectiveSocial >= tSocial && effectiveSocial >= (nonSocialMax + socialLeadMargin))
            {
                return new AdvancedIntent
                {
                    Kind = AdvancedIntentKind.Socialize,
                    Score = effectiveSocial,
                    Reason = socialOverloaded ? "priority.socialize.overloaded_soft" : "priority.socialize"
                };
            }
            // “并列区”偏向社交：惩罚后 social 仍与最高非社交接近时优先社交（窗口需盖住 pSocial≈0.05）。
            if (effectiveSocial >= socialFloor
                && effectiveSocial >= tSocial * 0.96f
                && effectiveSocial >= (nonSocialMax - 0.02f))
            {
                return new AdvancedIntent
                {
                    Kind = AdvancedIntentKind.Socialize,
                    Score = effectiveSocial,
                    Reason = socialOverloaded ? "priority.socialize.near_tie.overloaded_soft" : "priority.socialize.near_tie"
                };
            }
            // 原始驱动已并列/社交略高：不因单次重复惩罚把本帧打成「社交劣势」。
            if (!socialOverloaded
                && socialBeforePenalty >= tSocial * 0.90f
                && socialBeforePenalty >= restBeforePenalty - 0.02f
                && effectiveSocial >= nonSocialMax - 0.03f)
            {
                return new AdvancedIntent
                {
                    Kind = AdvancedIntentKind.Socialize,
                    Score = effectiveSocial,
                    Reason = "priority.socialize.raw_tie_guard"
                };
            }

            // 社交不够“压倒性”时，允许次优非社交意图出头。
            if (rest >= tRest * 0.82f &&
                rest >= work + 0.04f &&
                rest >= solitude + 0.04f &&
                rest >= effectiveSocial + 0.06f)
            {
                return new AdvancedIntent { Kind = AdvancedIntentKind.Rest, Score = rest, Reason = "priority.rest.soft" };
            }
            if (work >= tWork * 0.82f && work >= solitude + 0.03f && work >= rest - 0.03f)
            {
                return new AdvancedIntent { Kind = AdvancedIntentKind.Work, Score = work, Reason = "priority.work.soft" };
            }
            if (solitude >= tSolitude * 0.82f && solitude >= rest - 0.03f)
            {
                return new AdvancedIntent { Kind = AdvancedIntentKind.Solitude, Score = solitude, Reason = "priority.solitude.soft" };
            }

            // 如果非社交驱动已经达到中等强度，走分流选择，避免长期固定在单一意图。
            if (nonSocialMax >= 0.38f)
            {
                return _fallbackDiversified(
                    state,
                    effectiveSocial,
                    rest,
                    work,
                    solitude,
                    socialOverloaded,
                    socialBeforePenalty,
                    restBeforePenalty,
                    tSocial,
                    socialFloor);
            }

            var maxScore = Math.Max(Math.Max(effectiveSocial, rest), Math.Max(work, solitude));

            // 兜底策略：只要总体驱动达到中等强度，就不再返回 None，
            // 直接选择当前最高意图，避免系统长期卡在 idle。
            const float fallbackIntentFloor = 0.34f;
            if (maxScore >= fallbackIntentFloor)
            {
                return _fallbackDiversified(
                    state,
                    effectiveSocial,
                    rest,
                    work,
                    solitude,
                    socialOverloaded,
                    socialBeforePenalty,
                    restBeforePenalty,
                    tSocial,
                    socialFloor);
            }

            return new AdvancedIntent
            {
                Kind = AdvancedIntentKind.None,
                Score = maxScore,
                Reason = "priority.idle"
            };
        }

        private static float _get(AdvancedNpcState state, string key)
        {
            if (state == null || state.Drives == null || string.IsNullOrEmpty(key)) return 0f;
            return state.Drives.TryGetValue(key, out var v) ? Math.Clamp(v, 0f, 1f) : 0f;
        }

        private static float _getSignal(AdvancedNpcState state, string key)
        {
            if (state == null || state.Signals == null || string.IsNullOrEmpty(key)) return 0f;
            return state.Signals.TryGetValue(key, out var v) ? Math.Clamp(v, 0f, 1f) : 0f;
        }

        private static void _applySignalDriveShim(
            AdvancedNpcState state,
            ref float social,
            ref float rest,
            ref float work,
            ref float solitude)
        {
            var fe = _getSignal(state, AdvancedSignalKeys.FePressure);
            var load = _getSignal(state, AdvancedSignalKeys.ConsciousnessLoad);
            var personalitySocial = _getSignal(state, AdvancedSignalKeys.PersonalitySocialApproach);
            var personalityFocus = _getSignal(state, AdvancedSignalKeys.PersonalityTaskFocus);
            var personalityReactive = _getSignal(state, AdvancedSignalKeys.PersonalityEmotionalReactivity);
            social = Math.Clamp(social + personalitySocial * PersonalitySocialGain, 0f, 1f);
            solitude = Math.Clamp(solitude + fe * FePressureSolitudeGain, 0f, 1f);
            work = Math.Clamp(work + personalityFocus * PersonalityWorkGain, 0f, 1f);
            rest = Math.Clamp(rest + load * ConsciousnessLoadRestGain + personalityReactive * PersonalityReactivityRestGain, 0f, 1f);
            solitude = Math.Clamp(solitude + personalityReactive * PersonalityReactivitySolitudeGain, 0f, 1f);
        }

        private static void _applyRepeatPenalty(
            AdvancedNpcState state,
            ref float social,
            ref float rest,
            ref float work,
            ref float solitude)
        {
            if (state == null) return;
            const float pSocial = 0.05f;
            const float pOther = 0.08f;
            switch (state.Intent.Kind)
            {
                case AdvancedIntentKind.Socialize: social = Math.Max(0f, social - pSocial); break;
                case AdvancedIntentKind.Rest: rest = Math.Max(0f, rest - pOther); break;
                case AdvancedIntentKind.Work: work = Math.Max(0f, work - pOther); break;
                case AdvancedIntentKind.Solitude: solitude = Math.Max(0f, solitude - pOther); break;
            }
        }

        private static AdvancedIntent _fallbackDiversified(
            AdvancedNpcState state,
            float social,
            float rest,
            float work,
            float solitude,
            bool socialOverloaded,
            float socialBeforePenalty,
            float restBeforePenalty,
            float tSocial,
            float socialFloor)
        {
            var candidates = new List<(AdvancedIntentKind kind, float score)>
            {
                (AdvancedIntentKind.Rest, rest),
                (AdvancedIntentKind.Work, work),
                (AdvancedIntentKind.Solitude, solitude)
            };
            if (!socialOverloaded && social >= socialFloor)
            {
                candidates.Add((AdvancedIntentKind.Socialize, social));
            }

            var max = 0f;
            for (var i = 0; i < candidates.Count; i++) if (candidates[i].score > max) max = candidates[i].score;
            var near = new List<(AdvancedIntentKind kind, float score)>();
            for (var i = 0; i < candidates.Count; i++)
            {
                if (max - candidates[i].score <= 0.04f) near.Add(candidates[i]);
            }

            if (near.Count == 1)
            {
                return new AdvancedIntent { Kind = near[0].kind, Score = near[0].score, Reason = $"priority.{near[0].kind.ToString().ToLowerInvariant()}.fallback" };
            }

            // 并列且「原始社交」不低于休息时，不要随机到 Rest（否则会出现 social>rest 的日志与意图矛盾）。
            if (!socialOverloaded
                && social >= socialFloor
                && socialBeforePenalty >= tSocial * 0.92f
                && socialBeforePenalty >= restBeforePenalty - 0.01f)
            {
                for (var i = 0; i < near.Count; i++)
                {
                    if (near[i].kind != AdvancedIntentKind.Socialize) continue;
                    return new AdvancedIntent
                    {
                        Kind = AdvancedIntentKind.Socialize,
                        Score = social,
                        Reason = "priority.socialize.fallback_prefer_tie"
                    };
                }
            }

            var seed = Math.Abs((state?.Npc?.GetInstanceID() ?? 0) + DateTime.Now.Second);
            var pick = near[seed % near.Count];
            return new AdvancedIntent { Kind = pick.kind, Score = pick.score, Reason = $"priority.{pick.kind.ToString().ToLowerInvariant()}.fallback_mix" };
        }
    }
}
