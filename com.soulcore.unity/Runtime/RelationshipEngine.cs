using System;
using System.Collections.Generic;
using System.Linq;

namespace SoulCore
{
    public class Relationship
    {
        public string TargetId;
        public int Value;
        public readonly List<Dictionary<string, object>> History = new();
        public Dictionary<string, object> LastChange;

        public string GetLevelName()
        {
            var levelNames = new Dictionary<int, string>
            {
                { -10, "死敌" }, { -8, "仇敌" }, { -6, "憎恨" }, { -4, "厌恶" }, { -2, "不满" },
                { 0, "陌生" },
                { 2, "友善" }, { 4, "好感" }, { 6, "信赖" }, { 8, "挚友" }, { 10, "灵魂伴侣" }
            };
            foreach (var th in levelNames.Keys.OrderByDescending(x => x))
            {
                if (Value >= th) return levelNames[th];
            }
            return "陌生";
        }
    }

    public class RelationshipEngine
    {
        public bool EnablePersonalityEffect = true;
        public readonly Dictionary<string, int> Stats = new() { { "total_changes", 0 }, { "threshold_triggers", 0 } };
        public readonly Dictionary<string, Dictionary<string, Relationship>> Relationships = new();

        private static readonly Dictionary<string, int> DefaultEvents = new()
        {
            { "help", 1 }, { "rescue", 2 }, { "gift", 1 }, { "praise", 1 }, { "keep_promise", 1 },
            { "betray", -2 }, { "insult", -1 }, { "lie", -1 }, { "break_promise", -2 }, { "ignore_help", -1 },
            { "rob", -2 }, { "fight", -2 }, { "share", 1 }, { "protect", 2 },
            { "teach", 1 }, { "learn_from", 1 }, { "disagree", -1 }, { "agree", 1 }
        };

        public Relationship Get(string agentId, string targetId)
        {
            if (!Relationships.TryGetValue(agentId, out var map))
            {
                map = new Dictionary<string, Relationship>();
                Relationships[agentId] = map;
            }
            if (!map.TryGetValue(targetId, out var r))
            {
                r = new Relationship { TargetId = targetId, Value = 0 };
                map[targetId] = r;
            }
            return r;
        }

        public int GetValue(string agentId, string targetId) => Get(agentId, targetId).Value;

        public string GetLevelName(string agentId, string targetId) => Get(agentId, targetId).GetLevelName();

        public Dictionary<string, object> ApplyEvent(
            string agentId, string targetId, string eventType, Dictionary<string, float> personality)
        {
            if (!DefaultEvents.TryGetValue(eventType, out var delta) || delta == 0)
            {
                return new Dictionary<string, object>
                {
                    { "old", GetValue(agentId, targetId) },
                    { "new", GetValue(agentId, targetId) },
                    { "delta", 0 }
                };
            }
            return Change(agentId, targetId, delta, "事件:" + eventType, personality);
        }

        public Dictionary<string, object> Change(
            string agentId, string targetId, int delta, string reason, Dictionary<string, float> personality)
        {
            var rel = Get(agentId, targetId);
            var oldV = rel.Value;
            var adj = delta;
            if (EnablePersonalityEffect && personality != null)
            {
                float c(string k, float d) => personality.TryGetValue(k, out var v) ? v : d;
                if (delta > 0)
                {
                    if (c("compassion", 0.5f) > 0.7f) adj += 1;
                    else if (c("compassion", 0.5f) < 0.3f) adj -= 1;
                }
                if (delta < 0)
                {
                    if (c("selfishness", 0.5f) > 0.7f) adj -= 1;
                    else if (c("selfishness", 0.5f) < 0.3f) adj += 1;
                }
            }
            var newV = Math.Clamp(oldV + adj, -10, 10);
            rel.Value = newV;
            var record = new Dictionary<string, object>
            {
                { "delta", delta }, { "adjusted", adj }, { "old", oldV }, { "new", newV }, { "reason", reason }
            };
            rel.History.Add(record);
            if (rel.History.Count > 20) rel.History.RemoveAt(0);
            rel.LastChange = record;
            Stats["total_changes"]++;
            return record;
        }

        public void DailyUpdate(System.Random rng)
        {
            foreach (var kv in Relationships)
            {
                foreach (var r in kv.Value.Values)
                {
                    if (r.Value > 0 && rng.NextDouble() < 0.1) r.Value = Math.Max(0, r.Value - 1);
                    else if (r.Value < 0 && rng.NextDouble() < 0.05) r.Value = Math.Min(0, r.Value + 1);
                }
            }
        }

        public int CountForAgent(string agentId) =>
            Relationships.TryGetValue(agentId, out var m) ? m.Count : 0;
    }
}
