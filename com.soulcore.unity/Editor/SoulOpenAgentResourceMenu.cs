using SoulCore;
using UnityEditor;
using UnityEngine;
namespace SoulCore.Editor
{
    /// <summary>
    /// 显式菜单：避免只在 Hierarchy 里找「Create」；<see cref="CreateAssetMenuAttribute"/> 在 **Project(项目) 窗口** 的 **创建** 下。
    /// </summary>
    public static class SoulOpenAgentResourceMenu
    {
        [MenuItem("魂核/资源/创建 开放式智能体 API 配置", false, 5)]
        public static void CreateOpenAgentTj() => _create("SoulOpenAgentProfile");

        private static void _create(string baseName)
        {
            var folder = "Assets";
            if (Selection.activeObject != null)
            {
                var p = AssetDatabase.GetAssetPath(Selection.activeObject);
                // 若选的是层级或场景内对象，p 可能为空；勿用 Path.GetDirectoryName("")，会抛 Invalid path
                if (!string.IsNullOrEmpty(p))
                {
                    if (AssetDatabase.IsValidFolder(p)) folder = p;
                    else
                    {
                        var i = p.LastIndexOf('/');
                        if (i > 0)
                        {
                            var dir = p.Substring(0, i);
                            if (AssetDatabase.IsValidFolder(dir)) folder = dir;
                        }
                    }
                }
            }

            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{baseName}.asset");
            var a = ScriptableObject.CreateInstance<SoulOpenAgentProfile>();
            AssetDatabase.CreateAsset(a, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = a;
        }
    }
}
