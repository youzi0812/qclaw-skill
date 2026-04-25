using UnityEditor;
using UnityEngine;

namespace SoulCore.Editor
{
    /// <summary>Samples~ 在 Project 中不可见，部分用户不知道需在 Package Manager 里点 Import。</summary>
    public static class SoulCoreSampleHelpMenu
    {
        [MenuItem("魂核/帮助/如何导入基础输入示例…", false, 200)]
        public static void ShowSampleImportHelp()
        {
            EditorUtility.DisplayDialog(
                "如何导入「基础输入演示」",
                "示例在包内「Samples~」中，在 Project 里可能看不到，需从包管理器导入：\n\n" +
                "1. 菜单 窗口 (Window) → 包管理器 (Package Manager)\n" +
                "2. 左上角选择「在项目中 (In Project)」\n" +
                "3. 在列表中选中：意识流魂核 (com.soulcore.unity)\n" +
                "4. 在右侧包详情**向下滚到**「Samples / 样本 / 示例」\n" +
                "5. 在「基础输入演示」行点击「导入 (Import)」\n\n" +
                "导入后会在 Assets/Samples/… 下出现脚本。详见：\n" +
                "Documentation~/Samples-基础输入演示.md",
                "知道了");
        }
    }
}
