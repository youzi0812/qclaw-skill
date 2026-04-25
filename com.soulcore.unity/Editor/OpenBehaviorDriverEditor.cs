using System.Reflection;
using SoulCore;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SoulCore.Editor
{
    [CustomEditor(typeof(OpenBehaviorDriver))]
    [CanEditMultipleObjects]
    public sealed class OpenBehaviorDriverEditor : UnityEditor.Editor
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
                if (label != null)
                {
                    if (prop.name == "_taskNpcLayerMask")
                    {
                        _drawLocalizedLayerMaskField(prop, label);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(prop, label, true);
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(prop, true);
                }
            }
            while (prop.NextVisible(false));

            _drawPersonalityPresetPanel();
            _drawRateLimitWarningsAfterAllRateFields();
            _drawTaskNpcPolicyWarnings();
            _drawTaskPeriodPersonalityWarnings();
            _drawTaskNpcQuickStartChecklist();
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 仅顶层字段：从 <see cref="OpenBehaviorDriver"/> 的 FieldInfo 读取
        /// <see cref="InspectorNameAttribute"/> / <see cref="TooltipAttribute"/>。
        /// 不依赖 <see cref="SerializedProperty.displayName"/>（自定义 Inspector 里常为英文 Nicify 名）。
        /// </summary>
        private static GUIContent _guiContentFromFieldAttributes(SerializedProperty prop)
        {
            if (prop.depth != 0)
            {
                return null;
            }

            var field = typeof(OpenBehaviorDriver).GetField(prop.name, _fieldFlags);
            if (field == null)
            {
                return null;
            }

            var inspectorName = field.GetCustomAttribute<InspectorNameAttribute>();
            var tooltip = field.GetCustomAttribute<TooltipAttribute>();
            var title = inspectorName != null ? inspectorName.displayName : prop.displayName;
            var tip = tooltip != null ? tooltip.tooltip : string.Empty;
            return new GUIContent(title, tip);
        }

        private static void _drawLocalizedLayerMaskField(SerializedProperty prop, GUIContent label)
        {
            if (prop == null)
            {
                return;
            }

            var layers = InternalEditorUtility.layers;
            if (layers == null || layers.Length == 0)
            {
                EditorGUILayout.PropertyField(prop, label, true);
                return;
            }

            var localized = new string[layers.Length];
            for (var i = 0; i < layers.Length; i++)
            {
                localized[i] = _localizeLayerName(layers[i]);
            }

            var currentMask = prop.intValue;
            var shownMask = 0;
            for (var i = 0; i < layers.Length; i++)
            {
                var layerIndex = LayerMask.NameToLayer(layers[i]);
                if (layerIndex < 0) continue;
                if ((currentMask & (1 << layerIndex)) != 0)
                {
                    shownMask |= 1 << i;
                }
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

        private static string _localizeLayerName(string layerName)
        {
            return layerName switch
            {
                "Nothing" => "无",
                "Everything" => "全部",
                "Default" => "默认",
                "TransparentFX" => "透明特效",
                "Ignore Raycast" => "忽略射线",
                "Water" => "水体",
                "UI" => "界面",
                _ => layerName
            };
        }

        /// <summary>
        /// 合并两条黄条为一条（避免同屏两条橙色重复感）。
        /// </summary>
        private void _drawRateLimitWarningsAfterAllRateFields()
        {
            var pNpc = serializedObject.FindProperty("_maxSocializePerNpcPerMinute");
            var pPair = serializedObject.FindProperty("_maxSocializePerPairPerMinute");
            if (pNpc == null || pPair == null) return;

            var npcHigh = !pNpc.hasMultipleDifferentValues && pNpc.intValue >= 5;
            var pairHigh = !pPair.hasMultipleDifferentValues && pPair.intValue >= 3;
            if (!npcHigh && !pairHigh) return;

            if (npcHigh && pairHigh)
            {
                EditorGUILayout.HelpBox(
                    "当前「每 NPC」与「每对」上限同时偏高，叠加后对话会非常密：\n" +
                    "• 每 NPC ≥5：单人容易变成「话痨」，破坏沉浸。\n" +
                    "• 每对 ≥3：固定 A-B 易出现「二人转」式复读。\n" +
                    "若仅为压测可保留；正式体验建议把每 NPC 调到 2～4、每对调到 1～2。",
                    MessageType.Warning);
                return;
            }

            if (npcHigh)
            {
                EditorGUILayout.HelpBox(
                    "每 NPC 每分钟上限 ≥5：该 NPC 容易变成「话痨」，可能破坏沉浸感。建议常规 2～4。",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.HelpBox(
                "每对 NPC 每分钟上限 ≥3：A-B 组合可能出现「二人转」式复读。建议常规 1～2。",
                MessageType.Warning);
        }

        private void _drawTaskNpcPolicyWarnings()
        {
            var pPolicy = serializedObject.FindProperty("_taskNpcSocialPolicy");
            if (pPolicy == null || pPolicy.hasMultipleDifferentValues) return;

            var pMatchMode = serializedObject.FindProperty("_taskNpcMatchMode");
            var pTaskTags = serializedObject.FindProperty("_taskNpcTags");
            var pTaskLayerMask = serializedObject.FindProperty("_taskNpcLayerMask");
            var pTaskDirectList = serializedObject.FindProperty("_taskNpcDirectList");
            var pUrgentTags = serializedObject.FindProperty("_urgentTaskTags");
            var pRange = serializedObject.FindProperty("_taskNpcSocializeRangeMeters");
            var pUseProvider = serializedObject.FindProperty("_useCustomTaskConstraintProvider");
            var pProvider = serializedObject.FindProperty("_taskConstraintProviderBehaviour");
            var policyIndex = pPolicy.enumValueIndex;
            var matchMode = (pMatchMode != null && !pMatchMode.hasMultipleDifferentValues) ? pMatchMode.enumValueIndex : 0;
            var taskTagsEmpty = pTaskTags == null || pTaskTags.arraySize <= 0;
            var taskLayerEmpty = pTaskLayerMask == null || pTaskLayerMask.intValue == 0;
            var taskDirectEmpty = pTaskDirectList == null || pTaskDirectList.arraySize <= 0;
            var providerEnabled = pUseProvider != null && !pUseProvider.hasMultipleDifferentValues && pUseProvider.boolValue;
            var providerMissing = providerEnabled && (pProvider == null || pProvider.objectReferenceValue == null);

            var canMatchTaskNpc = providerEnabled
                ? !providerMissing
                : matchMode switch
                {
                    0 => !taskTagsEmpty || !taskDirectEmpty, // 仅标签
                    1 => !taskLayerEmpty || !taskDirectEmpty, // 仅图层
                    _ => !taskTagsEmpty || !taskLayerEmpty || !taskDirectEmpty // 标签或图层
                };
            if (policyIndex == 1)
            {
                if (!canMatchTaskNpc)
                {
                    EditorGUILayout.HelpBox(
                        "当前策略为「范围限制」，但任务NPC匹配条件为空（或自定义组件未指定）：不会命中任何任务NPC，等同未启用。可补充任务标签/图层或「任务NPC直指定」。运行时会继续输出 taskCount=0 警告。",
                        MessageType.Warning);
                }
                if (pRange != null && !pRange.hasMultipleDifferentValues && pRange.floatValue < 1.5f)
                {
                    EditorGUILayout.HelpBox(
                        "「任务NPC社交半径」较小（<1.5m），可能导致任务NPC几乎不主动社交。",
                        MessageType.Info);
                }
                if (!providerEnabled && (matchMode == 1 || matchMode == 2) && taskLayerEmpty)
                {
                    EditorGUILayout.HelpBox(
                        "当前任务判定包含图层，但「任务NPC图层」仍是 Nothing（空掩码）：图层条件不会命中任何NPC。",
                        MessageType.Warning);
                }
                return;
            }

            if (policyIndex != 2) return;
            var urgentTagsEmpty = pUrgentTags == null || pUrgentTags.arraySize <= 0;
            if (providerEnabled && providerMissing)
            {
                EditorGUILayout.HelpBox(
                    "已开启「启用自定义任务判定」，但未指定实现组件：将无法正确执行任务静默规则。",
                    MessageType.Warning);
                return;
            }
            if (!providerEnabled && (!canMatchTaskNpc || urgentTagsEmpty))
            {
                EditorGUILayout.HelpBox(
                    "当前策略为「忙碌静默」，请确保能命中任务NPC，并配置「紧急任务标签」或「紧急任务NPC直指定」，否则静默规则无法正确命中。运行时会输出 taskCount/urgentCount 警告帮助定位。",
                    MessageType.Warning);
            }
            if (!providerEnabled && (matchMode == 1 || matchMode == 2) && taskLayerEmpty)
            {
                EditorGUILayout.HelpBox(
                    "当前任务判定包含图层，但「任务NPC图层」仍是 Nothing（空掩码）：图层条件不会命中任何NPC。",
                    MessageType.Warning);
            }
        }

        private static void _drawTaskNpcQuickStartChecklist()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "任务NPC社交约束接入清单（推荐）：\n" +
                "1) 先选「任务NPC社交策略」：范围限制 / 忙碌静默。\n" +
                "2) 配置任务命中条件：任务标签/任务图层/任务NPC直指定（三选一或组合），或挂自定义判定组件。\n" +
                "3) 做一次 A/B 验证：选 1 个任务NPC + 1 个普通NPC对照；若用「忙碌静默」，任务NPC需命中紧急任务标签或紧急任务NPC直指定。运行时若未命中会自动输出 taskCount/urgentCount 警告。",
                MessageType.Info);
        }

        private void _drawTaskPeriodPersonalityWarnings()
        {
            var pEnableTaskOverride = serializedObject.FindProperty("_taskPeriodPersonalityWeightOverride");
            if (pEnableTaskOverride == null || pEnableTaskOverride.hasMultipleDifferentValues || !pEnableTaskOverride.boolValue)
            {
                return;
            }

            var pTaskTargetMul = serializedObject.FindProperty("_taskPeriodTargetSelectionWeightMultiplier");
            var pTaskStyleMul = serializedObject.FindProperty("_taskPeriodStyleWeightMultiplier");
            var pTaskTopicMul = serializedObject.FindProperty("_taskPeriodTopicWeightMultiplier");
            var pEnableUrgent = serializedObject.FindProperty("_urgentTaskExtraPersonalityWeightOverride");
            var pUrgentTargetMul = serializedObject.FindProperty("_urgentTaskTargetSelectionWeightMultiplier");
            var pUrgentStyleMul = serializedObject.FindProperty("_urgentTaskStyleWeightMultiplier");
            var pUrgentTopicMul = serializedObject.FindProperty("_urgentTaskTopicWeightMultiplier");

            if (pTaskTargetMul != null && !pTaskTargetMul.hasMultipleDifferentValues && pTaskTargetMul.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("任务期目标选择权重乘子为 0：任务NPC将完全关闭目标选择中的人格影响。", MessageType.Warning);
            }
            if (pTaskStyleMul != null && !pTaskStyleMul.hasMultipleDifferentValues && pTaskStyleMul.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("任务期语气权重乘子为 0：任务NPC将完全关闭语气中的人格影响。", MessageType.Warning);
            }
            if (pTaskTopicMul != null && !pTaskTopicMul.hasMultipleDifferentValues && pTaskTopicMul.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("任务期话题权重乘子为 0：任务NPC将完全关闭话题偏好中的人格影响。", MessageType.Warning);
            }

            if (pEnableUrgent == null || pEnableUrgent.hasMultipleDifferentValues || !pEnableUrgent.boolValue)
            {
                return;
            }
            if (pUrgentTargetMul != null && !pUrgentTargetMul.hasMultipleDifferentValues && pUrgentTargetMul.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("紧急任务目标选择额外乘子为 0：紧急任务NPC将完全关闭目标选择中的人格影响。", MessageType.Warning);
            }
            if (pUrgentStyleMul != null && !pUrgentStyleMul.hasMultipleDifferentValues && pUrgentStyleMul.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("紧急任务语气额外乘子为 0：紧急任务NPC将完全关闭语气中的人格影响。", MessageType.Warning);
            }
            if (pUrgentTopicMul != null && !pUrgentTopicMul.hasMultipleDifferentValues && pUrgentTopicMul.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("紧急任务话题额外乘子为 0：紧急任务NPC将完全关闭话题偏好中的人格影响。", MessageType.Warning);
            }
        }

        private void _drawPersonalityPresetPanel()
        {
            var pPreset = serializedObject.FindProperty("_personalityPreset");
            if (pPreset == null) return;
            var presetMissing = pPreset.objectReferenceValue == null && pPreset.objectReferenceInstanceIDValue != 0;

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("人格倾向预设面板", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(pPreset, new GUIContent("当前预设", "选择 OpenBehaviorPersonalityPreset 资产。"));

            if (presetMissing)
            {
                EditorGUILayout.HelpBox("当前预设引用已丢失（Missing）。可先清除，再重新选择预设资产。", MessageType.Warning);
                if (GUILayout.Button("清除缺失预设引用"))
                {
                    pPreset.objectReferenceValue = null;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("应用预设到当前组件"))
                {
                    _applyPresetToTargets(pPreset.objectReferenceValue as OpenBehaviorPersonalityPreset);
                }

                if (GUILayout.Button("定位预设资产") && pPreset.objectReferenceValue != null)
                {
                    EditorGUIUtility.PingObject(pPreset.objectReferenceValue);
                }
            }

            if (GUILayout.Button("一键创建默认预设并应用"))
            {
                _createDefaultPresetAssignAndApply(pPreset);
            }

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("快速模板", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("社牛型")) _applyBuiltInPresetToTargets("social_extrovert");
                if (GUILayout.Button("社恐型")) _applyBuiltInPresetToTargets("social_avoidant");
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("高冷型")) _applyBuiltInPresetToTargets("cool_detached");
                if (GUILayout.Button("热心肠")) _applyBuiltInPresetToTargets("warm_helper");
            }

            EditorGUILayout.HelpBox(
                "说明：点击“应用预设到当前组件”会把预设中的人格表达参数（目标选择/语气/话题/任务期/紧急期）写入当前 OpenBehaviorDriver。",
                MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void _applyPresetToTargets(OpenBehaviorPersonalityPreset preset)
        {
            if (preset == null)
            {
                EditorUtility.DisplayDialog("未选择预设", "请先在“当前预设”中指定一个人格倾向预设资产。", "知道了");
                return;
            }

            for (var i = 0; i < targets.Length; i++)
            {
                if (targets[i] is not OpenBehaviorDriver driver) continue;
                Undo.RecordObject(driver, "应用人格倾向预设");
                driver.ApplyPersonalityPreset(preset);
                EditorUtility.SetDirty(driver);
            }
        }

        private void _applyBuiltInPresetToTargets(string presetId)
        {
            var preset = ScriptableObject.CreateInstance<OpenBehaviorPersonalityPreset>();
            string fileBaseName;
            switch (presetId)
            {
                case "social_extrovert":
                    fileBaseName = "人格预设_社牛型";
                    preset.presetName = "社牛型";
                    preset.description = "高外向高亲和，社交表达更主动。";
                    preset.personalityTargetSelectionWeight = 0.75f;
                    preset.personalityStyleWeight = 0.72f;
                    preset.personalityTopicWeight = 0.70f;
                    preset.taskPeriodPersonalityWeightOverride = true;
                    preset.taskPeriodTargetSelectionWeightMultiplier = 0.95f;
                    preset.taskPeriodStyleWeightMultiplier = 0.95f;
                    preset.taskPeriodTopicWeightMultiplier = 0.95f;
                    preset.urgentTaskExtraPersonalityWeightOverride = true;
                    preset.urgentTaskTargetSelectionWeightMultiplier = 0.80f;
                    preset.urgentTaskStyleWeightMultiplier = 0.80f;
                    preset.urgentTaskTopicWeightMultiplier = 0.80f;
                    break;
                case "social_avoidant":
                    fileBaseName = "人格预设_社恐型";
                    preset.presetName = "社恐型";
                    preset.description = "降低社交表达强度，任务期进一步收敛。";
                    preset.personalityTargetSelectionWeight = 0.35f;
                    preset.personalityStyleWeight = 0.32f;
                    preset.personalityTopicWeight = 0.30f;
                    preset.taskPeriodPersonalityWeightOverride = true;
                    preset.taskPeriodTargetSelectionWeightMultiplier = 0.70f;
                    preset.taskPeriodStyleWeightMultiplier = 0.70f;
                    preset.taskPeriodTopicWeightMultiplier = 0.70f;
                    preset.urgentTaskExtraPersonalityWeightOverride = true;
                    preset.urgentTaskTargetSelectionWeightMultiplier = 0.65f;
                    preset.urgentTaskStyleWeightMultiplier = 0.65f;
                    preset.urgentTaskTopicWeightMultiplier = 0.65f;
                    break;
                case "cool_detached":
                    fileBaseName = "人格预设_高冷型";
                    preset.presetName = "高冷型";
                    preset.description = "语气与话题偏好更克制，优先弱表达。";
                    preset.personalityTargetSelectionWeight = 0.40f;
                    preset.personalityStyleWeight = 0.25f;
                    preset.personalityTopicWeight = 0.28f;
                    preset.taskPeriodPersonalityWeightOverride = true;
                    preset.taskPeriodTargetSelectionWeightMultiplier = 0.75f;
                    preset.taskPeriodStyleWeightMultiplier = 0.60f;
                    preset.taskPeriodTopicWeightMultiplier = 0.65f;
                    preset.urgentTaskExtraPersonalityWeightOverride = true;
                    preset.urgentTaskTargetSelectionWeightMultiplier = 0.65f;
                    preset.urgentTaskStyleWeightMultiplier = 0.55f;
                    preset.urgentTaskTopicWeightMultiplier = 0.55f;
                    break;
                case "warm_helper":
                    fileBaseName = "人格预设_热心肠";
                    preset.presetName = "热心肠";
                    preset.description = "偏关怀表达，语气更柔和。";
                    preset.personalityTargetSelectionWeight = 0.62f;
                    preset.personalityStyleWeight = 0.70f;
                    preset.personalityTopicWeight = 0.74f;
                    preset.taskPeriodPersonalityWeightOverride = true;
                    preset.taskPeriodTargetSelectionWeightMultiplier = 0.90f;
                    preset.taskPeriodStyleWeightMultiplier = 0.92f;
                    preset.taskPeriodTopicWeightMultiplier = 0.95f;
                    preset.urgentTaskExtraPersonalityWeightOverride = true;
                    preset.urgentTaskTargetSelectionWeightMultiplier = 0.82f;
                    preset.urgentTaskStyleWeightMultiplier = 0.85f;
                    preset.urgentTaskTopicWeightMultiplier = 0.88f;
                    break;
                default:
                    Object.DestroyImmediate(preset);
                    return;
            }

            var createdAsset = _createPresetAsset(preset, fileBaseName);
            _applyPresetToTargets(preset);
            _assignPresetToTargets(createdAsset);
            EditorGUIUtility.PingObject(createdAsset);
            Object.DestroyImmediate(preset);
        }

        private void _createDefaultPresetAssignAndApply(SerializedProperty pPreset)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "创建人格倾向预设",
                "OpenBehaviorPersonalityPreset_Default",
                "asset",
                "请选择保存路径");
            if (string.IsNullOrEmpty(path)) return;

            var asset = ScriptableObject.CreateInstance<OpenBehaviorPersonalityPreset>();
            asset.presetName = "默认预设";
            asset.description = "由 OpenBehaviorDriver 面板一键创建。";
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            pPreset.objectReferenceValue = asset;
            _applyPresetToTargets(asset);
            EditorGUIUtility.PingObject(asset);
        }

        private static OpenBehaviorPersonalityPreset _createPresetAsset(OpenBehaviorPersonalityPreset source, string fileBaseName)
        {
            const string folder = "Assets/SoulCore/Presets";
            if (!AssetDatabase.IsValidFolder("Assets/SoulCore"))
            {
                AssetDatabase.CreateFolder("Assets", "SoulCore");
            }
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/SoulCore", "Presets");
            }

            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{fileBaseName}.asset");
            var asset = ScriptableObject.CreateInstance<OpenBehaviorPersonalityPreset>();
            EditorUtility.CopySerialized(source, asset);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return asset;
        }

        private void _assignPresetToTargets(OpenBehaviorPersonalityPreset preset)
        {
            if (preset == null) return;
            for (var i = 0; i < targets.Length; i++)
            {
                if (targets[i] is not OpenBehaviorDriver driver) continue;
                Undo.RecordObject(driver, "绑定人格倾向预设资产");
                var so = new SerializedObject(driver);
                var p = so.FindProperty("_personalityPreset");
                if (p == null) continue;
                p.objectReferenceValue = preset;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(driver);
            }
        }
    }
}
