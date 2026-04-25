using System.Collections.Generic;

namespace SoulCore
{
    /// <summary>
    /// 将决策中的内部 action / 情绪键转为界面与日志用短名（团结/中文运行时统一用中文）。</summary>
    public static class SoulCoreActionDisplay
    {
        private static readonly Dictionary<string, string> ZhAction = new()
        {
            ["help_others"] = "帮一把",
            ["self_first"] = "先顾自己",
            ["open_up"] = "敞开心扉",
            ["listen"] = "听你说",
            ["deflect"] = "不太想说",
            ["silence"] = "沉默",
            ["leave_now"] = "马上走",
            ["prepare"] = "准备一下",
            ["wait"] = "等等看",
            ["stay"] = "不走了",
            ["go_rescue"] = "去救人",
            ["call_help"] = "求助",
            ["stay_safe"] = "保护好自己",
            ["learn"] = "学一下",
            ["ignore"] = "先不管",
            ["proceed"] = "继续",
            ["avoid"] = "避开",
            ["dead"] = "死亡",
            ["skipped_out_of_range"] = "未感知(距离)",
            ["skipped_throttle"] = "未感知(频限)"
        };

        private static readonly Dictionary<string, string> ZhEmotion = new()
        {
            ["joy"] = "喜悦",
            ["fear"] = "恐惧",
            ["anger"] = "愤怒",
            ["hope"] = "希望",
            ["despair"] = "绝望",
            ["gratitude"] = "感激",
            ["pride"] = "自豪",
            ["curiosity_emotion"] = "好奇",
            ["—"] = "无",
            ["none"] = "无"
        };

        public static string Action(string code)
        {
            if (string.IsNullOrEmpty(code)) return SoulCoreLocalization.IsChineseForRuntime() ? "—" : "—";
            if (SoulCoreLocalization.IsChineseForRuntime() && ZhAction.TryGetValue(code, out var z)) return z;
            return code;
        }

        public static string Emotion(string code)
        {
            if (string.IsNullOrEmpty(code)) return SoulCoreLocalization.IsChineseForRuntime() ? "无" : "none";
            if (SoulCoreLocalization.IsChineseForRuntime() && ZhEmotion.TryGetValue(code, out var z)) return z;
            return code;
        }
    }
}
