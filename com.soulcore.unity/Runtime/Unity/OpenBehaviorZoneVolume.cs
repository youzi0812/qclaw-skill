using UnityEngine;

namespace SoulCore
{
    /// <summary>
    /// 区域体积：用球形范围为进入区域的 NPC 选择 <see cref="OpenBehaviorZoneProfile"/>。
    /// </summary>
    [AddComponentMenu("魂核/开放行为/区域体积")]
    [DisallowMultipleComponent]
    public sealed class OpenBehaviorZoneVolume : MonoBehaviour
    {
        [SerializeField, InspectorName("区域配置"), Tooltip("进入该体积范围时应用的区域行为配置。")]
        private OpenBehaviorZoneProfile _profile;

        [SerializeField, Min(0.1f), InspectorName("半径(米)"), Tooltip("球形作用范围半径。")]
        private float _radius = 12f;

        [SerializeField, InspectorName("优先级"), Tooltip("多个区域重叠时，优先级更高者生效。")]
        private int _priority;

        [SerializeField, InspectorName("忽略高度(Y)"), Tooltip("开启后只按 XZ 平面距离判定区域。")]
        private bool _ignoreY = true;

        public OpenBehaviorZoneProfile Profile => _profile;
        public int Priority => _priority;

        public bool Contains(Vector3 worldPos)
        {
            var c = transform.position;
            var d = worldPos - c;
            if (_ignoreY) d.y = 0f;
            return d.sqrMagnitude <= _radius * _radius;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.28f);
            if (_ignoreY)
            {
                var prev = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.Euler(90f, 0f, 0f), Vector3.one);
                Gizmos.DrawWireSphere(Vector3.zero, _radius);
                Gizmos.matrix = prev;
                return;
            }

            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
