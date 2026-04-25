using System.Collections.Generic;
using UnityEngine;

namespace SoulCore
{
    /// <summary>
    /// 运行时文案。团结渠道恒为简体中文；Unity 国际渠道由 PlayerPrefs「SoulCore.Lang」：zh / en（默认 en）。
    /// </summary>
    public static class SoulCoreLocalization
    {
        public const string PlayerPrefsLangKey = "SoulCore.Lang";
        public const string LangZh = "zh";
        public const string LangEn = "en";

        private static readonly Dictionary<string, (string zh, string en)> Table = new()
        {
            [SoulCoreI18nKeys.RtSkipOutOfRange] = ("目标不在关注距离内", "Target is outside focus distance"),
            [SoulCoreI18nKeys.RtSkipThrottle] = ("感知被频率限制", "Perception throttled by rate limit"),
            [SoulCoreI18nKeys.RtModuleDecisionOff] = ("决策模块已关闭", "Decision module is disabled"),
            ["rt.soul.deadLine"] = ("{0}已经不在人世", "{0} is no longer alive"),
            ["rt.soul.emotionNone"] = ("无", "none"),
            ["rt.soul.relOff"] = ("关系模块已关闭", "Relationship module is disabled"),
            ["rt.soul.birth"] = ("魂核{0}诞生", "Soul {0} — born"),
            ["rt.soul.eventMem"] = ("{0}事件，强度{1:F1}", "{0} event, intensity {1:F1}"),
            ["rt.soul.dreamMem"] = ("梦境: {0}", "Dream: {0}"),
            ["rt.explain.title"] = ("=== {0}的灵魂 ===", "=== Soul: {0} ==="),
            ["rt.explain.personality"] = ("【人格】", "[Personality]"),
            ["rt.explain.emotion"] = ("【情感】", "[Emotion]")
        };

        public static bool IsTuanjieChannel => SoulCoreEngineInfo.Channel == SoulCoreChannel.TuanjieEditor;

        /// <summary>团结渠道恒为中文；Unity 国际渠道由 PlayerPrefs「SoulCore.Lang」决定，未设置时默认英文。</summary>
        public static bool IsChineseForRuntime()
        {
            if (IsTuanjieChannel) return true;
            var defaultLang = SoulCoreEngineInfo.Channel == SoulCoreChannel.UnityEditorInternational ? LangEn : LangZh;
            return PlayerPrefs.GetString(PlayerPrefsLangKey, defaultLang) == LangZh;
        }

        public static void SetRuntimeLanguage(string lang)
        {
            PlayerPrefs.SetString(PlayerPrefsLangKey, lang == LangZh ? LangZh : LangEn);
            PlayerPrefs.Save();
        }

        public static string Rt(string key)
        {
            if (!Table.TryGetValue(key, out var pair))
                return key;
            return IsChineseForRuntime() ? pair.zh : pair.en;
        }

        public static string RtFormat(string key, params object[] args)
        {
            var fmt = Rt(key);
            try { return string.Format(fmt, args); }
            catch { return fmt; }
        }
    }
}
