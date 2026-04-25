using SoulCore;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace SoulCore.Editor
{
    public static class SoulCoreSettingsProvider
    {
        private const string Path = "Project/Soul Core 魂核";

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new SettingsProvider(Path, SettingsScope.Project)
            {
                label = "Soul Core 魂核",
                keywords = new[] { "Soul", "魂核", "语言", "Language", "Tuanjie", "团结", "API", "智能体", "Open" },
                guiHandler = _onGui,
            };
        }

        private static void _onGui(string searchContext)
        {
            var tj = SoulCoreEngineInfo.Channel == SoulCoreChannel.TuanjieEditor;
            EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.settings.title"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("当前产品档位: " + SoulCoreEdition.TierLabelZh, MessageType.None);
            EditorGUILayout.Space();

            if (tj)
            {
                EditorGUILayout.HelpBox(SoulCoreEditorI18n.Ed("ed.settings.tuanjie"), MessageType.Info);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.lang.zh"), EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.HelpBox(SoulCoreEditorI18n.Ed("ed.settings.unity"), MessageType.None);
                EditorGUILayout.Space();

                var cur = EditorPrefs.GetString(SoulCoreEditorI18n.EditorPrefsLangKey, SoulCoreLocalization.LangZh);
                var idx = cur == SoulCoreLocalization.LangZh ? 0 : 1;
                var labels = new[] { SoulCoreEditorI18n.Ed("ed.lang.zh"), SoulCoreEditorI18n.Ed("ed.lang.en") };
                EditorGUI.BeginChangeCheck();
                var n = EditorGUILayout.Popup("Language / 语言", idx, labels);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetString(SoulCoreEditorI18n.EditorPrefsLangKey, n == 0 ? SoulCoreLocalization.LangZh : SoulCoreLocalization.LangEn);
                    SoulCoreEditorI18n.ApplyEditorLangToPlayerPrefs();
                }

                EditorGUILayout.Space();
                if (GUILayout.Button("Sync runtime (PlayerPrefs) / 同步运行时 PlayerPrefs"))
                    SoulCoreEditorI18n.ApplyEditorLangToPlayerPrefs();
            }

            EditorGUILayout.Space(10f);
            _drawOpenAgentSettings();
        }

        private static void _drawOpenAgentSettings()
        {
            EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.openAgent.title"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(SoulCoreEditorI18n.Ed("ed.openAgent.hint"), MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(SoulCoreEditorI18n.Ed("ed.openAgent.createDefault")))
            {
                const string resPath = "Assets/Resources/SoulOpenAgentDefault.asset";
                if (!Directory.Exists("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                var existing = AssetDatabase.LoadAssetAtPath<SoulOpenAgentProfile>(resPath);
                if (existing != null) Selection.activeObject = existing;
                else
                {
                    var a = ScriptableObject.CreateInstance<SoulOpenAgentProfile>();
                    AssetDatabase.CreateAsset(a, resPath);
                    AssetDatabase.SaveAssets();
                    Selection.activeObject = a;
                }
            }
            if (GUILayout.Button(SoulCoreEditorI18n.Ed("ed.openAgent.createAnywhere")))
            {
                var a = ScriptableObject.CreateInstance<SoulOpenAgentProfile>();
                var p = AssetDatabase.GenerateUniqueAssetPath("Assets/SoulOpenAgentProfile.asset");
                AssetDatabase.CreateAsset(a, p);
                AssetDatabase.SaveAssets();
                Selection.activeObject = a;
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
