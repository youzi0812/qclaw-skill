using UnityEditor;
using UnityEngine;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build;
#endif

namespace SoulCore.Editor
{
    public static class SoulCoreEditionBuildMenu
    {
        private const string SymbolFree = "SOUL_EDITION_FREE";
        private const string SymbolPro = "SOUL_EDITION_PRO";

        [MenuItem("魂核/产品版本/切换为 免费版 编译 (上架免费资产前执行)")]
        public static void SetFree() => _setAll(true);

        [MenuItem("魂核/产品版本/切换为 标准版 编译 (默认)")]
        public static void SetStandard() => _setAll(false);

        [MenuItem("魂核/产品版本/切换为 高级版 6.1.8 编译 (SOUL_EDITION_PRO)")]
        public static void SetPro() => _setAll(null);

        /// <param name="freeMode">true=免费 false=标准 null=高级</param>
        private static void _setAll(bool? freeMode)
        {
#if UNITY_2021_2_OR_NEWER
            var targets = new[]
            {
                NamedBuildTarget.Standalone, NamedBuildTarget.iOS, NamedBuildTarget.Android, NamedBuildTarget.WebGL
            };
            foreach (var n in targets)
            {
                var def = PlayerSettings.GetScriptingDefineSymbols(n);
                def = _remove(_remove(def, SymbolFree), SymbolPro);
                if (freeMode == true) def = _append(def, SymbolFree);
                else if (freeMode == null) def = _append(def, SymbolPro);
                PlayerSettings.SetScriptingDefineSymbols(n, def);
            }
            var msg = freeMode switch
            {
                true => "已切换为 免费版 4.0（SOUL_EDITION_FREE）。",
                false => "已切换为 标准版 4.0（默认，无版本宏）。",
                _ => "已切换为 高级版 6.1.8（SOUL_EDITION_PRO）。"
            };
            Debug.Log("[魂核] " + msg);
            EditorUtility.RequestScriptReload();
#else
            Debug.LogWarning("[魂核] 请手动在 Player Settings 中加入或移除: SOUL_EDITION_FREE / SOUL_EDITION_PRO");
#endif
        }

        private static string _append(string all, string symbol)
        {
            if (string.IsNullOrEmpty(all)) return symbol;
            if (all.IndexOf(symbol, System.StringComparison.Ordinal) >= 0) return all;
            return all + ";" + symbol;
        }

        private static string _remove(string all, string symbol)
        {
            if (string.IsNullOrEmpty(all)) all = string.Empty;
            var list = all.Split(';');
            var sb = new System.Text.StringBuilder();
            foreach (var p in list)
            {
                var t = p.Trim();
                if (t.Length == 0 || t == symbol) continue;
                if (sb.Length > 0) sb.Append(';');
                sb.Append(t);
            }
            return sb.ToString();
        }
    }
}
