using System.Reflection;
using SoulCore;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SoulCore.Editor
{
    [CustomEditor(typeof(OpenBehaviorTaskConstraintSample))]
    [CanEditMultipleObjects]
    public sealed class OpenBehaviorTaskConstraintSampleEditor : UnityEditor.Editor
    {
        private static readonly BindingFlags _fieldFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var prop = serializedObject.GetIterator();
            if (!prop.NextVisible(true))
            {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            do
            {
                if (prop.name == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(prop, true);
                    }
                    continue;
                }

                var label = _guiContentFromFieldAttributes(prop);
                if (prop.name == "_taskNpcLayerMask")
                {
                    _drawLocalizedLayerMaskField(prop, label ?? new GUIContent(prop.displayName));
                }
                else if (label != null)
                {
                    EditorGUILayout.PropertyField(prop, label, true);
                }
                else
                {
                    EditorGUILayout.PropertyField(prop, true);
                }
            }
            while (prop.NextVisible(false));

            serializedObject.ApplyModifiedProperties();
        }

        private static GUIContent _guiContentFromFieldAttributes(SerializedProperty prop)
        {
            if (prop.depth != 0) return null;
            var field = typeof(OpenBehaviorTaskConstraintSample).GetField(prop.name, _fieldFlags);
            if (field == null) return null;
            var inspectorName = field.GetCustomAttribute<InspectorNameAttribute>();
            var tooltip = field.GetCustomAttribute<TooltipAttribute>();
            var title = inspectorName != null ? inspectorName.displayName : prop.displayName;
            var tip = tooltip != null ? tooltip.tooltip : string.Empty;
            return new GUIContent(title, tip);
        }

        private static void _drawLocalizedLayerMaskField(SerializedProperty prop, GUIContent label)
        {
            var layers = InternalEditorUtility.layers;
            if (layers == null || layers.Length == 0)
            {
                EditorGUILayout.PropertyField(prop, label, true);
                return;
            }

            var localized = new string[layers.Length];
            for (var i = 0; i < layers.Length; i++)
            {
                localized[i] = layers[i] switch
                {
                    "Nothing" => "无",
                    "Everything" => "全部",
                    "Default" => "默认",
                    "TransparentFX" => "透明特效",
                    "Ignore Raycast" => "忽略射线",
                    "Water" => "水体",
                    "UI" => "界面",
                    _ => layers[i]
                };
            }

            var currentMask = prop.intValue;
            var shownMask = 0;
            for (var i = 0; i < layers.Length; i++)
            {
                var layerIndex = LayerMask.NameToLayer(layers[i]);
                if (layerIndex < 0) continue;
                if ((currentMask & (1 << layerIndex)) != 0) shownMask |= 1 << i;
            }

            var newShownMask = EditorGUILayout.MaskField(label, shownMask, localized);
            if (newShownMask == shownMask) return;

            var newMask = 0;
            for (var i = 0; i < layers.Length; i++)
            {
                if ((newShownMask & (1 << i)) == 0) continue;
                var layerIndex = LayerMask.NameToLayer(layers[i]);
                if (layerIndex < 0) continue;
                newMask |= 1 << layerIndex;
            }
            prop.intValue = newMask;
        }
    }
}
