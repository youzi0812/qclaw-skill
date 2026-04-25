using System.IO;
using UnityEditor;
using UnityEngine;

namespace SoulCore.Editor
{
    public static class SoulCoreUnityPackageExportMenu
    {
        private const string PackageRoot = "Packages/com.soulcore.unity";
        private const string TestScenePath = "Assets/Scenes/OpenBehavior_6_1_8_Setup.unity";
        private const string OutputDir = "C:/Users/U/Desktop/游戏插件/release";

        [MenuItem("魂核/产品版本/导出 当前版本为 unitypackage")]
        public static void ExportCurrentEditionUnitypackage()
        {
            ExportWithName(_suggestFileName());
        }

        private static string _suggestFileName()
        {
            var edition = SoulCoreEdition.IsFree ? "4.0-free" : (SoulCoreEdition.IsPro ? "6.1.8-pro" : "4.0-standard");
            return $"com.soulcore.unity-{edition}.unitypackage";
        }

        private static void ExportWithName(string fileName)
        {
            try
            {
                if (!AssetDatabase.IsValidFolder(PackageRoot))
                {
                    Debug.LogError($"[SoulCore/Export] 未找到包目录：{PackageRoot}");
                    return;
                }

                Directory.CreateDirectory(OutputDir);
                var outPath = Path.Combine(OutputDir, fileName).Replace("\\", "/");
                if (File.Exists(outPath)) File.Delete(outPath);

                var exportAssets = _buildExportAssetList();
                AssetDatabase.ExportPackage(
                    exportAssets,
                    outPath,
                    ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);

                Debug.Log($"[SoulCore/Export] 导出完成：{outPath}\n包含测试场景：{System.Array.IndexOf(exportAssets, TestScenePath) >= 0}");
                EditorUtility.RevealInFinder(outPath);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SoulCore/Export] 导出失败：{e.Message}");
            }
        }

        private static string[] _buildExportAssetList()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScenePath) != null)
            {
                return new[] { PackageRoot, TestScenePath };
            }

            Debug.LogWarning($"[SoulCore/Export] 未找到测试场景：{TestScenePath}，本次仅导出插件包内容。");
            return new[] { PackageRoot };
        }
    }
}
