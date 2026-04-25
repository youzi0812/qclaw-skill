using SoulCore;
using UnityEditor;
using UnityEngine;

namespace SoulCore.Editor
{
    [CustomEditor(typeof(SoulNPCConfig))]
    public class SoulNPCConfigEditor : UnityEditor.Editor
    {
        private const string FoldKey = "SoulCore.SoulNPCConfigEditor.showAdvanced";
        private static readonly string[] PresetValues = { "default", "kind", "brave", "selfish", "creative", "grumpy" };
        private static readonly string[] PresetLabels =
        {
            "默认（default）",
            "热心（kind）",
            "勇敢（brave）",
            "自利（selfish）",
            "创意（creative）",
            "急躁（grumpy）"
        };

        private static void _draw(SerializedObject so, string prop, string label, string tip = null)
        {
            EditorGUILayout.PropertyField(
                so.FindProperty(prop),
                string.IsNullOrEmpty(tip) ? new GUIContent(label) : new GUIContent(label, tip));
        }

        private static void DrawPresetPopup(SerializedObject so)
        {
            var p = so.FindProperty("personalityPreset");
            if (p == null) return;

            var current = string.IsNullOrEmpty(p.stringValue) ? "default" : p.stringValue;
            var idx = 0;
            for (var i = 0; i < PresetValues.Length; i++)
            {
                if (PresetValues[i] != current) continue;
                idx = i;
                break;
            }

            var newIdx = EditorGUILayout.Popup(
                new GUIContent("人格预设", "选择内置人格预设。建议演示时保持 default，然后用“人格个性化”拉开差异。"),
                idx,
                PresetLabels);
            if (newIdx >= 0 && newIdx < PresetValues.Length)
            {
                p.stringValue = PresetValues[newIdx];
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var p = serializedObject;

            EditorGUILayout.LabelField("魂核 NPC 配置（极简）", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                p.FindProperty("suggestedDisplayName"),
                new GUIContent("名称", "可选：当 SoulNPC 显示名称为空时使用"));

            EditorGUILayout.PropertyField(
                p.FindProperty("oneLinePersonality"),
                new GUIContent("一句话性格", "例如“他是个自私的人”"));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("初始关系", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                p.FindProperty("initialRelations"),
                new GUIContent("关系列表"),
                true);

            EditorGUILayout.Space(6f);
            var showAdv = SessionState.GetBool(FoldKey, false);
            showAdv = EditorGUILayout.ToggleLeft("显示高级人格参数（默认隐藏）", showAdv);
            SessionState.SetBool(FoldKey, showAdv);

            if (showAdv)
            {
                EditorGUILayout.Space(4f);
                DrawPresetPopup(p);
                var profileProp = p.FindProperty("personalityProfile");
                var profile = profileProp != null ? profileProp.objectReferenceValue as SoulPersonalityProfile : null;
                EditorGUILayout.LabelField("人格个性化（可选）", profile != null ? $"已绑定：{profile.name}" : "未绑定");
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("打开人格细化面板"))
                {
                    SoulPersonalityProfileWindow.Open(profile, target as SoulNPCConfig);
                }
                if (GUILayout.Button("使用当前选中"))
                {
                    if (Selection.activeObject is SoulPersonalityProfile selected)
                    {
                        profileProp.objectReferenceValue = selected;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("提示", "请先在 Project 面板选中一个 SoulPersonalityProfile 资产。", "确定");
                    }
                }
                if (GUILayout.Button("清除绑定"))
                {
                    profileProp.objectReferenceValue = null;
                    var inlineEnabled = p.FindProperty("inlinePersonalityEnabled");
                    if (inlineEnabled != null) inlineEnabled.boolValue = false;
                }
                EditorGUILayout.EndHorizontal();
                _draw(p, "maxMemories", "记忆上限");
                _draw(p, "emotionDailyDecayScale", "情绪日衰减系数");
                _draw(p, "flavorOutput", "人性化辅文案来源");

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("决策权重", EditorStyles.boldLabel);
                _draw(p, "decisionWeightPersonality", "人格权重");
                _draw(p, "decisionWeightEmotion", "情绪权重");
                _draw(p, "decisionWeightMemory", "记忆权重");
                _draw(p, "decisionWeightResource", "资源权重");

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("模块开关", EditorStyles.boldLabel);
                _draw(p, "moduleEmotion", "情绪模块");
                _draw(p, "moduleMemory", "记忆模块");
                _draw(p, "moduleDecision", "决策模块");
                _draw(p, "moduleRelationship", "关系模块");
                _draw(p, "moduleInterest", "兴趣模块");
                _draw(p, "moduleCuriosity", "好奇模块");
                _draw(p, "moduleHabit", "习惯模块");
                _draw(p, "moduleSerendipity", "机缘模块");
                _draw(p, "moduleEmotionInfect", "情绪感染模块");
                _draw(p, "moduleHumor", "幽默模块");
                _draw(p, "moduleComplain", "抱怨模块");
                _draw(p, "moduleEmbarrassment", "尴尬模块");
                _draw(p, "moduleExploration", "探索模块");
                _draw(p, "modulePreference", "偏好模块");
                _draw(p, "moduleNostalgia", "怀旧模块");
                _draw(p, "moduleAwe", "敬畏模块");
                _draw(p, "moduleIntuition", "直觉模块");
                _draw(p, "moduleInspiration", "灵感模块");
                _draw(p, "moduleDream", "梦境模块");
                _draw(p, "moduleConsistency", "一致性模块");
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
