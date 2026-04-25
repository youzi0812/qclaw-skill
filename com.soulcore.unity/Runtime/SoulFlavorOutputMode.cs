namespace SoulCore
{
    /// <summary>
    /// 「人性化」辅文案（玩笑/吐槽/灵感槽位等）来源：引擎内置随机模板，或留空由游戏自行填充。
    /// 不影响决策主链（action / explanation / emotion 等）。
    /// </summary>
    public enum SoulFlavorOutputMode
    {
        /// <summary>使用内置轻量模板填充 SoulDecision 的辅字段。</summary>
        BuiltInTemplates = 0,
        /// <summary>不生成辅文案，这些字段为 null，可由你的对话/UI 系统自设。</summary>
        CustomLeaveSlotsEmpty = 1
    }
}
