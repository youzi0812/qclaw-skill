using SoulCore;
using UnityEngine;

namespace SoulCore.Samples
{
    /// <summary>
    /// 将本脚本挂在任意对象上，场景中需有带 SoulNPC 的物体与主摄像机/玩家。
    /// 1=对话(感谢) 2=赠礼 3=侮辱 — 对列表中所有 SoulNPC 广播 Perceive。
    /// 也可作为复制到自己项目里的最简参考。
    /// </summary>
    public class BasicDemoInput : MonoBehaviour
    {
        [SerializeField] private SoulNPC[] _npcs;
        [SerializeField] private string _playerId = "player_1";
        [SerializeField] private bool _bypassDistanceThrottle;
        [Tooltip("Start 时创建 SoulProcessScheduler，使 TryEnqueuePerceive 走分帧队列")]
        [SerializeField] private bool _ensureProcessScheduler = true;

        private void Start()
        {
            if (_ensureProcessScheduler) SoulProcessScheduler.EnsureInHierarchy();
        }

        private void Update()
        {
            if (_npcs == null || _npcs.Length == 0) return;
            if (Input.GetKeyDown(KeyCode.Alpha1)) _broadcast("conversation", "谢谢你的帮助，我很感激", 0.6f);
            if (Input.GetKeyDown(KeyCode.Alpha2)) _broadcast("gift", "送你一份礼物", 0.7f);
            if (Input.GetKeyDown(KeyCode.Alpha3)) _broadcast("insult", "我真是看不起你", 0.8f);
        }

        private void _broadcast(string type, string content, float intensity)
        {
            var ctx = new PerceptionContext(type, content, intensity, _playerId);
            foreach (var n in _npcs)
            {
                if (n == null) continue;
                if (n.TryEnqueuePerceive(ctx, d => _log(n, d, true), _bypassDistanceThrottle)) continue;
                var d2 = n.Perceive(ctx, _bypassDistanceThrottle);
                _log(n, d2, false);
            }
        }

        private static void _log(SoulNPC n, SoulDecision d, bool fromSchedulerQueue)
        {
            if (n == null) return;
            var name = n.CharacterName;
            var tag = fromSchedulerQueue ? "[魂核/分帧]" : "[魂核]";
            if (d == null)
            {
                Debug.Log($"{tag} {name} → (无返回)", n);
                return;
            }
            if (SoulCoreLocalization.IsChineseForRuntime())
            {
                Debug.Log(
                    $"{tag} {name} → {SoulCoreActionDisplay.Action(d.Action)} | {d.Explanation} | 情绪={SoulCoreActionDisplay.Emotion(d.Emotion)}",
                    n);
            }
            else
            {
                Debug.Log($"{tag} {name} → {d.Action} | {d.Explanation} | emo={d.Emotion}", n);
            }
        }
    }
}
