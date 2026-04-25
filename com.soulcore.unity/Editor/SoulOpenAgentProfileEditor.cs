using SoulCore;
using UnityEditor;
using UnityEngine;

namespace SoulCore.Editor
{
    [CustomEditor(typeof(SoulOpenAgentProfile))]
    public class SoulOpenAgentProfileEditor : UnityEditor.Editor
    {
        private string _apiKeyBuffer;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var obj = (SoulOpenAgentProfile)target;
            if (_apiKeyBuffer == null) _apiKeyBuffer = obj.ApiKey ?? "";

            var prop = serializedObject.GetIterator();
            var enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.name == "m_Script")
                {
                    EditorGUILayout.PropertyField(prop);
                    continue;
                }
                if (prop.name == "ApiKey")
                {
                    EditorGUI.BeginChangeCheck();
                    _apiKeyBuffer = EditorGUILayout.PasswordField(
                        new GUIContent("API Key (密钥，输入时遮盖)", "勿提交到公开仓库；正式环境用密钥管理"),
                        _apiKeyBuffer);
                    if (EditorGUI.EndChangeCheck()) prop.stringValue = _apiKeyBuffer;
                    continue;
                }
                EditorGUILayout.PropertyField(prop, true);
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(
                SoulCoreEditorI18n.UseChinese
                    ? "本包不发起网络请求。在你方脚本中读取本资源，用 UnityWebRequest 等实现调用；与 Perceive/本地决策的关系由你方拼接。"
                    : "This package does not make HTTP calls. Use fields in your own networking code and merge with Perceive as needed.",
                MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
