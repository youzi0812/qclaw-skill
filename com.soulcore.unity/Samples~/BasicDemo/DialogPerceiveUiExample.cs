using SoulCore;
using UnityEngine;
using UnityEngine.UI;

namespace SoulCore.Samples
{
    /// <summary>
    /// 第①步最小示例：将 Canvas 上 InputField 的内容发给 <see cref="SoulNPC.Perceive"/>，把 <see cref="SoulDecision.Explanation"/> 写回 Text。
    /// 在 Inspector 里拖：魂核 NPC、InputField、Button、Text；给 Button 的 OnClick 绑定 <see cref="OnSendClicked"/>。
    /// 不依赖 <see cref="BasicDemoInput"/>，可与键盘示例二选一或并存（并存时会两侧都响）。
    /// </summary>
    public class DialogPerceiveUiExample : MonoBehaviour
    {
        [SerializeField] private SoulNPC _npc;
        [SerializeField] private InputField _input;
        [SerializeField] private Text _output;
        [SerializeField] private string _playerId = "player_1";
        [Tooltip("有调度器时优先排队，减少单帧压力")]
        [SerializeField] private bool _preferQueued = true;

        public void OnSendClicked()
        {
            if (_npc == null) return;
            var text = _input != null ? _input.text : "";
            if (string.IsNullOrEmpty(text)) text = "……";

            var ctx = new PerceptionContext("conversation", text, 0.6f, _playerId);
            if (_preferQueued)
            {
                if (_npc.TryEnqueuePerceive(
                        ctx,
                        d => _setOut(d),
                        bypassBudget: false)) return;
            }
            var d = _npc.Perceive(ctx);
            _setOut(d);
        }

        private void _setOut(SoulDecision d)
        {
            if (_output == null) return;
            if (d == null) { _output.text = "(无结果)"; return; }
            _output.text = d.Explanation;
        }
    }
}
