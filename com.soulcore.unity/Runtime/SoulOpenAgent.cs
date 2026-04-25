using UnityEngine;

namespace SoulCore
{
    /// <summary>
    /// 解析「本 NPC 是否指定了智能体配置」与「工程默认（Resources 名 <see cref="DefaultResourceName"/>）」。
    /// </summary>
    public static class SoulOpenAgent
    {
        public const string DefaultResourceName = "SoulOpenAgentDefault";

        /// <summary>优先使用 NPC 上的覆盖，否则尝试 <c>Resources.Load&lt;SoulOpenAgentProfile&gt;(DefaultResourceName)</c>。</summary>
        public static SoulOpenAgentProfile ResolveFor(SoulNPC npc)
        {
            if (!SoulCoreEdition.CanUseOpenAgentApi) return null;
            if (npc != null)
            {
                var o = npc.OpenAgentOverride;
                if (o != null) return o;
            }
            return Resources.Load<SoulOpenAgentProfile>(DefaultResourceName);
        }

        public static bool HasUsableProfile(SoulNPC npc)
        {
            var p = ResolveFor(npc);
            return p != null && p.Enabled;
        }
    }
}
