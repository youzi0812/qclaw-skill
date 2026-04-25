using UnityEngine;

namespace SoulCore
{
    /// <summary>
    /// 开放式行为驱动器的人格倾向预设。
    /// 用于给策划提供可复用、可视化的人格表达参数模板。
    /// </summary>
    [CreateAssetMenu(
        fileName = "OpenBehaviorPersonalityPreset",
        menuName = "魂核/OpenBehavior/人格倾向预设",
        order = 2300)]
    public sealed class OpenBehaviorPersonalityPreset : ScriptableObject
    {
        [Header("预设信息")]
        [InspectorName("预设名称"), Tooltip("给策划辨识用，例如：社牛型 / 社恐型 / 高冷型。")]
        public string presetName = "默认预设";

        [TextArea(2, 6)]
        [InspectorName("预设说明"), Tooltip("该预设的设计意图与使用建议。")]
        public string description =
            "用于统一配置人格表达相关权重，可被 OpenBehaviorDriver 一键应用。";

        [Header("人格表达总开关")]
        [InspectorName("人格参与社交表达")] public bool personalityAffectsSocialExpression = true;
        [InspectorName("人格影响目标选择")] public bool personalityAffectsTargetSelection = true;
        [Range(0f, 1f), InspectorName("目标选择人格权重")] public float personalityTargetSelectionWeight = 0.5f;
        [InspectorName("人格影响对话语气")] public bool personalityAffectsStylePrefix = true;
        [Range(0f, 1f), InspectorName("语气人格权重")] public float personalityStyleWeight = 0.6f;
        [InspectorName("人格影响话题偏好")] public bool personalityAffectsTopicPreference = true;
        [Range(0f, 1f), InspectorName("话题人格权重")] public float personalityTopicWeight = 0.6f;

        [Header("任务期间人格权重覆盖")]
        [InspectorName("任务期间人格权重覆盖")] public bool taskPeriodPersonalityWeightOverride;
        [Range(0f, 2f), InspectorName("任务期目标选择权重乘子")] public float taskPeriodTargetSelectionWeightMultiplier = 0.7f;
        [Range(0f, 2f), InspectorName("任务期语气权重乘子")] public float taskPeriodStyleWeightMultiplier = 0.8f;
        [Range(0f, 2f), InspectorName("任务期话题权重乘子")] public float taskPeriodTopicWeightMultiplier = 0.8f;

        [Header("紧急任务额外人格权重覆盖")]
        [InspectorName("紧急任务额外人格权重覆盖")] public bool urgentTaskExtraPersonalityWeightOverride = true;
        [Range(0f, 2f), InspectorName("紧急任务目标选择额外乘子")] public float urgentTaskTargetSelectionWeightMultiplier = 0.7f;
        [Range(0f, 2f), InspectorName("紧急任务语气额外乘子")] public float urgentTaskStyleWeightMultiplier = 0.7f;
        [Range(0f, 2f), InspectorName("紧急任务话题额外乘子")] public float urgentTaskTopicWeightMultiplier = 0.7f;
    }
}
