using UnityEditor;
using UnityEngine;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build;
#endif

namespace SoulCore.Editor
{
    public static class SoulCoreChannelDefinesMenu
    {
        private const string Tuanjie = "SOULCORE_CHANNEL_TUANJIE";
        private const string UnityIntl = "SOULCORE_CHANNEL_UNITY";

        [MenuItem("魂核/渠道/设为团结 (添加 SOULCORE_CHANNEL_TUANJIE)")]
        public static void SetChannelTuanjie() => _apply(tuanjie: true);

        [MenuItem("魂核/渠道/设为 Unity 国际 (添加 SOULCORE_CHANNEL_UNITY)")]
        public static void SetChannelUnity() => _apply(tuanjie: false);

        [MenuItem("魂核/渠道/清除以上渠道宏")]
        public static void ClearChannelDefines() => _apply(clear: true);

        private static void _apply(bool? tuanjie = null, bool clear = false)
        {
#if UNITY_2021_2_OR_NEWER
            var targets = new[]
            {
                NamedBuildTarget.Standalone,
                NamedBuildTarget.iOS,
                NamedBuildTarget.Android,
                NamedBuildTarget.WebGL
            };
            foreach (var n in targets)
            {
                var s = PlayerSettings.GetScriptingDefineSymbols(n);
                string set;
                if (clear) set = _remove(_remove(s, Tuanjie), UnityIntl);
                else if (tuanjie == true)
                {
                    set = _remove(s, UnityIntl);
                    if (!_containsToken(set, Tuanjie))
                        set = string.IsNullOrEmpty(set) ? Tuanjie : set + ";" + Tuanjie;
                }
                else
                {
                    set = _remove(s, Tuanjie);
                    if (!_containsToken(set, UnityIntl))
                        set = string.IsNullOrEmpty(set) ? UnityIntl : set + ";" + UnityIntl;
                }
                PlayerSettings.SetScriptingDefineSymbols(n, set);
            }
            Debug.Log("[魂核] 已为 Standalone / iOS / Android / WebGL 设置 Scripting Define。其它平台请自行在 Player Settings 中同步或改用当前选中平台。");
            AssetDatabase.SaveAssets();
#else
            Debug.LogWarning("[魂核] 当前 Unity 版本较低，请手动在 Player Settings 中添加宏。");
#endif
        }

        private static string _remove(string all, string one)
        {
            if (string.IsNullOrEmpty(all)) return string.Empty;
            var parts = all.Split(';');
            var list = new System.Collections.Generic.List<string>(parts.Length);
            foreach (var p in parts)
            {
                var t = p.Trim();
                if (t.Length == 0 || t == one) continue;
                list.Add(t);
            }
            return string.Join(";", list);
        }

        private static bool _containsToken(string all, string token)
        {
            if (string.IsNullOrEmpty(all)) return false;
            foreach (var p in all.Split(';'))
            {
                if (p.Trim() == token) return true;
            }
            return false;
        }
    }
}
