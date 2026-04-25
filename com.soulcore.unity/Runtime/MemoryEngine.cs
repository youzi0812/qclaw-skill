using System;
using System.Collections.Generic;
using System.Linq;

namespace SoulCore
{
    public class MemoryEngine
    {
        private const int MaxPoolSize = 512;

        private readonly Stack<Memory> _pool = new();

        private readonly Dictionary<string, List<Memory>> _memories = new()
        {
            { "short_term", new List<Memory>() },
            { "long_term", new List<Memory>() },
            { "permanent", new List<Memory>() }
        };

        public readonly Dictionary<string, int> Stats = new() { { "total", 0 }, { "forgotten", 0 } };

        public int MaxMemories { get; private set; } = 200;

        public int PooledMemoryCount => _pool.Count;

        public void SetMaxMemories(int max)
        {
            MaxMemories = Math.Max(8, max);
            EnforceCap();
        }

        public int GetTotalCount() =>
            _memories["short_term"].Count + _memories["long_term"].Count + _memories["permanent"].Count;

        private double _now() => (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

        public Memory CreateMemory(
            string content,
            string type = "event",
            int importance = 5,
            string emotion = null)
        {
            var m = _rent();
            m.Id = Guid.NewGuid().ToString("N").Substring(0, 8);
            m.Content = content;
            m.Type = type;
            m.Importance = Math.Clamp(importance, 1, 10);
            m.CreatedAt = _now();
            m.Emotion = emotion;
            m.Strength = 1f;
            m.LastAccessed = null;
            m.AccessCount = 0;
            return m;
        }

        private Memory _rent() =>
            _pool.Count > 0 ? _pool.Pop() : new Memory { Associations = new List<string>(4) };

        private void _returnToPool(Memory m)
        {
            if (m == null) return;
            if (_pool.Count >= MaxPoolSize) return;
            m.PrepareReuse();
            _pool.Push(m);
        }

        public void Store(Memory memory)
        {
            if (memory.Importance >= 9) _memories["permanent"].Add(memory);
            else if (memory.Importance >= 5) _memories["long_term"].Add(memory);
            else _memories["short_term"].Add(memory);
            Stats["total"]++;
            EnforceCap();
        }

        public Memory StoreForced(string content, int importance = 9, string emotion = null)
        {
            var m = CreateMemory(content, "forced", importance, emotion);
            Store(m);
            return m;
        }

        public List<Memory> Recall(string cue, Dictionary<string, string> context = null, int limit = 5)
        {
            var results = new List<(float, Memory)>();
            foreach (var category in new[] { "permanent", "long_term", "short_term" })
            {
                foreach (var memory in _memories[category])
                {
                    var r = _calcRelevance(memory, cue, context);
                    if (r > 0.3f)
                    {
                        memory.Strengthen(_now());
                        results.Add((r, memory));
                    }
                }
            }
            return results
                .OrderByDescending(x => x.Item1)
                .Take(limit)
                .Select(x => x.Item2)
                .ToList();
        }

        private static float _calcRelevance(Memory memory, string cue, Dictionary<string, string> context)
        {
            float score = 0f;
            if (!string.IsNullOrEmpty(cue) && 
                !string.IsNullOrEmpty(memory.Content) && 
                memory.Content.IndexOf(cue, StringComparison.OrdinalIgnoreCase) >= 0)
                score += 0.4f;
            if (context != null && context.TryGetValue("type", out var t) && t == memory.Type)
                score += 0.3f;
            if (context != null && context.TryGetValue("emotion", out var e) && e == memory.Emotion)
                score += 0.2f;
            score += memory.Strength * 0.2f;
            score += memory.Importance / 50f;
            return Math.Min(1f, score);
        }

        public void EnforceCap()
        {
            var guard = 0;
            while (GetTotalCount() > MaxMemories && guard++ < 10000)
            {
                if (!TryEvictWeakest()) break;
            }
        }

        private bool TryEvictWeakest()
        {
            foreach (var bucket in new[] { "short_term", "long_term", "permanent" })
            {
                var list = _memories[bucket];
                if (list.Count == 0) continue;
                if (bucket == "permanent")
                {
                    var idx = -1;
                    var best = 11;
                    for (var i = 0; i < list.Count; i++)
                    {
                        if (list[i].Importance < best)
                        {
                            best = list[i].Importance;
                            idx = i;
                        }
                    }
                    if (idx >= 0 && best < 9)
                    {
                        var ev = list[idx];
                        list.RemoveAt(idx);
                        Stats["total"]--;
                        _returnToPool(ev);
                        return true;
                    }
                    break;
                }
                var weakest = 0f;
                var wi = 0;
                for (var i = 0; i < list.Count; i++)
                {
                    if (i == 0 || list[i].Strength < weakest)
                    {
                        weakest = list[i].Strength;
                        wi = i;
                    }
                }
                var evicted = list[wi];
                list.RemoveAt(wi);
                Stats["total"]--;
                _returnToPool(evicted);
                return true;
            }
            return false;
        }

        public void Forget()
        {
            var now = _now();
            foreach (var category in new[] { "short_term", "long_term" })
            {
                var toRemove = new List<Memory>();
                foreach (var memory in _memories[category])
                {
                    var last = memory.LastAccessed ?? memory.CreatedAt;
                    var days = (now - last) / 86400.0;
                    memory.Decay((float)days);
                    if (memory.IsForgotten()) toRemove.Add(memory);
                }
                foreach (var m in toRemove)
                {
                    _memories[category].Remove(m);
                    Stats["forgotten"]++;
                    Stats["total"]--;
                    _returnToPool(m);
                }
            }
        }
    }
}
