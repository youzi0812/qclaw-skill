using SoulCore;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SoulCore.Editor
{
    [CustomEditor(typeof(AdvancedOrchestrator))]
    [CanEditMultipleObjects]
    public sealed class AdvancedOrchestratorEditor : UnityEditor.Editor
    {
        private static readonly BindingFlags _fieldFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var prop = serializedObject.GetIterator();
            if (prop.NextVisible(true))
            {
                do
                {
                    var label = _guiContentFromFieldAttributes(prop);
                    using (new EditorGUI.DisabledScope(prop.name == "m_Script"))
                    {
                        if (label != null)
                        {
                            EditorGUILayout.PropertyField(prop, label, true);
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(prop, true);
                        }
                    }
                }
                while (prop.NextVisible(false));
            }

            serializedObject.ApplyModifiedProperties();

            if (targets.Length == 1)
            {
                EditorGUILayout.Space(8f);
                if (GUILayout.Button("打印人格摘要快照"))
                {
                    ((AdvancedOrchestrator)target).DumpPersonalitySnapshot();
                }
            }
        }

        private static GUIContent _guiContentFromFieldAttributes(SerializedProperty prop)
        {
            if (prop.depth != 0) return null;
            var field = typeof(AdvancedOrchestrator).GetField(prop.name, _fieldFlags);
            if (field == null) return null;
            var inspectorName = field.GetCustomAttribute<InspectorNameAttribute>();
            var tooltip = field.GetCustomAttribute<TooltipAttribute>();
            var title = inspectorName != null ? inspectorName.displayName : prop.displayName;
            var tip = tooltip != null ? tooltip.tooltip : string.Empty;
            return new GUIContent(title, tip);
        }
    }
}
