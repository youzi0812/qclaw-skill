using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulCore
{
    /// <summary>
    /// 分帧处理排队的 <see cref="SoulNPC.Perceive"/> 调用，减少单帧尖刺。
    /// 可挂场景物体或运行时 <see cref="EnsureInHierarchy"/> 自动创建。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("魂核/分帧调度器（感知队列）")]
    [DisallowMultipleComponent]
    public class SoulProcessScheduler : MonoBehaviour
    {
        [Tooltip("本帧最多处理的感知排队调用数量（其余顺延到后续帧）。")]
        [Min(1)]
        [SerializeField] private int _maxPerceptionsPerFrame = 8;

        private struct Job
        {
            public SoulNPC Npc;
            public PerceptionContext Context;
            public bool BypassBudget;
            public Action<SoulDecision> OnComplete;
        }

        private readonly Queue<Job> _queue = new();

        public static SoulProcessScheduler Instance { get; private set; }

        public int QueuedCount => _queue.Count;
        public int MaxPerceptionsPerFrame
        {
            get => _maxPerceptionsPerFrame;
            set => _maxPerceptionsPerFrame = Math.Max(1, value);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void _resetInstance() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[魂核] 场景中存在多个 SoulProcessScheduler，将销毁多余对象。", this);
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            var n = _maxPerceptionsPerFrame;
            while (n-- > 0 && _queue.Count > 0)
            {
                var j = _queue.Dequeue();
                if (j.Npc == null)
                {
                    j.OnComplete?.Invoke(null);
                    continue;
                }
                SoulDecision d;
                try
                {
                    d = j.Npc.Perceive(j.Context, j.BypassBudget);
                }
                catch (Exception e)
                {
                    Debug.LogException(e, j.Npc);
                    d = null;
                }
                j.OnComplete?.Invoke(d);
            }
        }

        public void Enqueue(
            SoulNPC npc,
            PerceptionContext context,
            bool bypassBudget = false,
            Action<SoulDecision> onComplete = null)
        {
            if (npc == null)
            {
                onComplete?.Invoke(null);
                return;
            }
            _queue.Enqueue(new Job
            {
                Npc = npc,
                Context = context,
                BypassBudget = bypassBudget,
                OnComplete = onComplete
            });
        }

        public void ClearQueue() => _queue.Clear();

        public static SoulProcessScheduler EnsureInHierarchy()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("SoulProcessScheduler");
            DontDestroyOnLoad(go);
            return go.AddComponent<SoulProcessScheduler>();
        }
    }
}
