using UnityEngine;

namespace SoulCore
{
    /// <summary>
    /// 开放世界区域行为配置：用于对进入该区域的 NPC 应用附加行为缩放或策略覆盖。
    /// </summary>
    [CreateAssetMenu(
        fileName = "OpenBehaviorZoneProfile",
        menuName = "魂核/OpenBehavior/区域行为配置",
        order = 2310)]
    public sealed class OpenBehaviorZoneProfile : ScriptableObject
    {
        [InspectorName("配置名称"), Tooltip("用于日志与调试识别。")]
        public string profileName = "默认区域";

        [Header("人格表达缩放")]
        [Range(0f, 2f), InspectorName("目标选择权重缩放")]
        public float personalityTargetWeightScale = 1f;
        [Range(0f, 2f), InspectorName("语气权重缩放")]
        public float personalityStyleWeightScale = 1f;
        [Range(0f, 2f), InspectorName("话题权重缩放")]
        public float personalityTopicWeightScale = 1f;

        [Header("任务社交策略覆盖")]
        [InspectorName("覆盖任务NPC社交策略")]
        public bool overrideTaskNpcSocialPolicy;
        [InspectorName("任务NPC社交策略")]
        public OpenBehaviorDriver.TaskNpcSocialPolicy taskNpcSocialPolicy = OpenBehaviorDriver.TaskNpcSocialPolicy.Unrestricted;

        [InspectorName("覆盖任务NPC社交半径")]
        public bool overrideTaskNpcSocializeRangeMeters;
        [Min(0.5f), InspectorName("任务NPC社交半径(米)")]
        public float taskNpcSocializeRangeMeters = 4f;
    }
}
