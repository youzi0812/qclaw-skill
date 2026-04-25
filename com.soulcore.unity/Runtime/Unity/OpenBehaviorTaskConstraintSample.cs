using System.Collections.Generic;
using UnityEngine;

namespace SoulCore
{
    /// <summary>
    /// 自定义任务判定示例组件（可直接挂到场景对象）。
    /// 将本组件拖到 OpenBehaviorDriver 的「自定义任务判定组件」即可启用。
    /// </summary>
    [AddComponentMenu("魂核/任务判定示例（开放行为）")]
    [DisallowMultipleComponent]
    public sealed class OpenBehaviorTaskConstraintSample : MonoBehaviour, IOpenBehaviorTaskConstraintProvider
    {
        [SerializeField, InspectorName("任务NPC标签"), Tooltip("命中这些标签视为任务NPC。")]
        private List<string> _taskNpcTags = new() { "QuestNpc" };

        [SerializeField, InspectorName("紧急任务标签"), Tooltip("命中这些标签视为紧急任务NPC。")]
        private List<string> _urgentTaskTags = new() { "QuestNpcUrgent" };

        [SerializeField, InspectorName("任务NPC图层"), Tooltip("命中该图层掩码也可视为任务NPC。")]
        private LayerMask _taskNpcLayerMask;

        [SerializeField, InspectorName("紧急任务需要同时是任务NPC"), Tooltip("开启后，紧急判定前先要求命中任务NPC条件。")]
        private bool _urgentMustAlsoBeTaskNpc = true;

        public bool IsTaskNpc(SoulNPC npc)
        {
            if (npc == null) return false;
            return _matchTags(npc.tag, _taskNpcTags) || _matchLayer(npc.gameObject, _taskNpcLayerMask);
        }

        public bool IsUrgentTaskNpc(SoulNPC npc)
        {
            if (npc == null) return false;
            if (_urgentMustAlsoBeTaskNpc && !IsTaskNpc(npc)) return false;
            return _matchTags(npc.tag, _urgentTaskTags);
        }

        private static bool _matchTags(string tag, List<string> tags)
        {
            if (string.IsNullOrEmpty(tag) || tags == null || tags.Count == 0) return false;
            for (var i = 0; i < tags.Count; i++)
            {
                var t = tags[i];
                if (string.IsNullOrWhiteSpace(t)) continue;
                if (string.Equals(tag, t.Trim(), System.StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static bool _matchLayer(GameObject go, LayerMask mask)
        {
            if (go == null) return false;
            var bit = 1 << go.layer;
            return (mask.value & bit) != 0;
        }
    }
}
