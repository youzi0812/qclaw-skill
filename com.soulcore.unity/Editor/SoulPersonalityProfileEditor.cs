using UnityEditor;
using UnityEngine;

namespace SoulCore.Editor
{
    [CustomEditor(typeof(SoulPersonalityProfile))]
    public class SoulPersonalityProfileEditor : UnityEditor.Editor
    {
        private static void Draw(SerializedObject so, string prop, string label, string tip = null)
        {
            var p = so.FindProperty(prop);
            if (p == null) return;
            EditorGUILayout.PropertyField(
                p,
                string.IsNullOrEmpty(tip) ? new GUIContent(label) : new GUIContent(label, tip));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("人格配置 Profile（4.0）", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("用于显式配置人格特质，避免只靠一句话推断导致演示差异不稳定。", MessageType.Info);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("基础风格", EditorStyles.boldLabel);
            Draw(serializedObject, "warmth", "温暖度");
            Draw(serializedObject, "compassion", "同理心");
            Draw(serializedObject, "trust", "信任倾向");
            Draw(serializedObject, "optimism", "乐观度");

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("行为倾向", EditorStyles.boldLabel);
            Draw(serializedObject, "courage", "勇敢度");
            Draw(serializedObject, "curiosity", "好奇度");
            Draw(serializedObject, "selfishness", "自保/自利倾向");
            Draw(serializedObject, "fearTendency", "恐惧倾向");

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("表达风格", EditorStyles.boldLabel);
            Draw(serializedObject, "patience", "耐心");
            Draw(serializedObject, "rationality", "理性");
            Draw(serializedObject, "creativity", "创造性");
            Draw(serializedObject, "persistence", "坚持度");

            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("应用预设：热心型")) ApplyPreset(kind: true);
            if (GUILayout.Button("应用预设：谨慎型")) ApplyPreset(kind: false);
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private void ApplyPreset(bool kind)
        {
            serializedObject.Update();
            if (kind)
            {
                Set("warmth", 0.85f);
                Set("compassion", 0.88f);
                Set("trust", 0.72f);
                Set("optimism", 0.7f);
                Set("courage", 0.56f);
                Set("curiosity", 0.6f);
                Set("selfishness", 0.2f);
                Set("fearTendency", 0.24f);
                Set("patience", 0.7f);
                Set("rationality", 0.55f);
                Set("creativity", 0.52f);
                Set("persistence", 0.64f);
            }
            else
            {
                Set("warmth", 0.45f);
                Set("compassion", 0.5f);
                Set("trust", 0.4f);
                Set("optimism", 0.42f);
                Set("courage", 0.4f);
                Set("curiosity", 0.36f);
                Set("selfishness", 0.62f);
                Set("fearTendency", 0.68f);
                Set("patience", 0.62f);
                Set("rationality", 0.76f);
                Set("creativity", 0.32f);
                Set("persistence", 0.58f);
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void Set(string prop, float value)
        {
            var p = serializedObject.FindProperty(prop);
            if (p != null) p.floatValue = value;
        }
    }
}
