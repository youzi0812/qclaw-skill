using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SoulCore
{
    public class Soul : ISoulState
    {
        public string Id { get; }
        public string Name { get; }
        public double CreatedAt;
        public bool IsAlive = true;
        public int Age;

        public PersonalityEngine Personality { get; }
        public EmotionEngine Emotion { get; }
        public readonly MemoryEngine Memory;
        public readonly DecisionEngine Decision;
        public readonly RelationshipEngine Relationship;

        public readonly InterestSystem Interest;
        public readonly CuriositySystem Curiosity;
        public readonly HabitSystem Habit;
        public readonly SerendipitySystem Serendipity;
        public readonly EmotionInfectionSystem EmotionInfect;
        public readonly HumorSystem Humor;
        public readonly ComplainSystem Complain;
        public readonly EmbarrassmentSystem Embarrassment;
        public readonly ExplorationSystem Exploration;
        public readonly PreferenceSystem Preference;
        public readonly NostalgiaSystem Nostalgia;
        public readonly AweSystem Awe;
        public readonly IntuitionSystemSimple Intuition;
        public readonly InspirationSystemSimple Inspiration;
        public readonly DreamSystemSimple Dream;
        public readonly ConsistencySystem Consistency;

        public object ResourceSystem;
        public object SpatialSystem;
        public object EventSystem;

        public readonly Dictionary<string, int> Stats = new()
        {
            { "experience_count", 0 }, { "decision_count", 0 },
            { "daily_interactions", 0 }
        };

        public Func<float> ResourceScarcityProvider;

        public SoulModuleFlags Modules = SoulModuleFlags.AllEnabled;
        public float EmotionDailyDecayScale = 1f;
        public SoulFlavorOutputMode FlavorOutput = SoulFlavorOutputMode.BuiltInTemplates;

        private bool _running = true;
        private readonly System.Random _rng;

        public float GetResourceScarcity() => ResourceScarcityProvider != null
            ? Math.Clamp(ResourceScarcityProvider(), 0f, 1f) : 0.5f;

        public Soul(string citizenId, string name, int randomSeed = 0)
        {
            Id = citizenId;
            Name = name;
            CreatedAt = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            _rng = randomSeed == 0 ? new System.Random() : new System.Random(randomSeed);

            Personality = new PersonalityEngine();
            Emotion = new EmotionEngine();
            Memory = new MemoryEngine();
            Relationship = new RelationshipEngine();
            Interest = new InterestSystem();
            Curiosity = new CuriositySystem();
            Habit = new HabitSystem(this);
            Serendipity = new SerendipitySystem(this);
            EmotionInfect = new EmotionInfectionSystem(this);
            Humor = new HumorSystem(this);
            Complain = new ComplainSystem(this);
            Embarrassment = new EmbarrassmentSystem(this);
            Exploration = new ExplorationSystem(this, Interest);
            Preference = new PreferenceSystem();
            Nostalgia = new NostalgiaSystem(this);
            Awe = new AweSystem(this);
            Intuition = new IntuitionSystemSimple(this);
            Inspiration = new InspirationSystemSimple(this);
            Dream = new DreamSystemSimple();
            Consistency = new ConsistencySystem(this);
            Decision = new DecisionEngine(this, randomSeed);

            Memory.StoreForced(SoulCoreLocalization.RtFormat("rt.soul.birth", name), 5);
        }

        public SoulDecision Perceive(PerceptionContext context)
        {
            if (!IsAlive)
            {
                return new SoulDecision
                {
                    Action = "dead",
                    Explanation = SoulCoreLocalization.RtFormat("rt.soul.deadLine", Name),
                    Confidence = 1f,
                    Emotion = SoulCoreLocalization.Rt("rt.soul.emotionNone"),
                    EmotionIntensity = 0,
                    RelevantMemories = new List<Memory>()
                };
            }

            var M = Modules;
            Stats["experience_count"]++;
            Stats["daily_interactions"]++;
            var personalityTraits = new Dictionary<string, float>(Personality.Traits);
            if (M.Emotion)
            {
                Emotion.Update(context.EventType, context.Intensity, context.Content ?? string.Empty, personalityTraits);
                if (M.EmotionInfect && !string.IsNullOrEmpty(context.UserEmotion))
                    EmotionInfect.Infect(context.UserEmotion, context.Intensity);
            }

            var memoryContext = new Dictionary<string, string>
            {
                { "content", context.Content ?? "" },
                { "target", context.TargetId ?? "" }
            };
            if (context.Metadata != null)
            {
                foreach (var kv in context.Metadata) memoryContext[kv.Key] = kv.Value;
            }
            var memories = M.Memory
                ? Memory.Recall(context.EventType, memoryContext, 5)
                : new List<Memory>();
            var situation = new Dictionary<string, string>
            {
                { "type", context.EventType },
                { "content", context.Content ?? "" },
                { "target_id", context.TargetId ?? "" }
            };
            var sitType = string.IsNullOrEmpty(context.EventType) ? "default" : context.EventType;
            string action, explanation;
            float conf;
            if (M.Decision)
            {
                (action, explanation, conf) = Decision.Decide(sitType, situation, memories);
                Stats["decision_count"]++;
            }
            else
            {
                action = "idle";
                explanation = SoulCoreLocalization.Rt(SoulCoreI18nKeys.RtModuleDecisionOff);
                conf = 0f;
            }

            var currentEmotion = M.Emotion ? Emotion.GetDominant() : "neutral";
            var emoI = M.Emotion ? Emotion.GetIntensity() : 0f;
            string intuition = null, inspiration = null, ser = null, humor = null, complaint = null;
            string embarrass = null, nostalgia = null, awe = null, exploration = null, cq = null;
            if (FlavorOutput == SoulFlavorOutputMode.BuiltInTemplates)
            {
                intuition = M.Intuition ? Intuition.Generate(situation) : null;
                inspiration = M.Inspiration ? Inspiration.Generate(situation) : null;
                ser = M.Serendipity ? Serendipity.Check(context.EventType) : null;
                humor = M.Humor ? Humor.TellJoke(situation) : null;
                complaint = M.Complain ? Complain.Complain(sitType) : null;
                embarrass = M.Embarrassment ? Embarrassment.Trigger(situation) : null;
                nostalgia = M.Nostalgia ? Nostalgia.Trigger() : null;
                awe = M.Awe ? Awe.ExpressAwe(context.Content) : null;
                exploration = M.Exploration ? Exploration.WantToExplore() : null;
                cq = M.Curiosity ? Curiosity.GenerateQuestion() : null;
            }

            if (M.Memory && context.Intensity > 0.6f)
            {
                var mem = Memory.CreateMemory(
                    SoulCoreLocalization.RtFormat("rt.soul.eventMem", context.EventType, context.Intensity),
                    "event", (int)(context.Intensity * 10), currentEmotion);
                Memory.Store(mem);
            }
            if (M.Interest && (context.EventType == "learn" || context.EventType == "watch" || context.EventType == "practice")
                && !string.IsNullOrEmpty(context.Content))
            {
                var snippet = context.Content.Length > 30 ? context.Content.Substring(0, 30) : context.Content;
                Interest.Update(snippet, 0.05f);
            }
            if (M.Consistency) Consistency.Update(situation, action);

            return new SoulDecision
            {
                Action = action,
                Explanation = explanation,
                Confidence = conf,
                Emotion = currentEmotion,
                EmotionIntensity = emoI,
                RelevantMemories = memories.Take(3).ToList(),
                Intuition = intuition,
                Inspiration = inspiration,
                Serendipity = ser,
                Humor = humor,
                Complaint = complaint,
                Embarrassment = embarrass,
                Nostalgia = nostalgia,
                Awe = awe,
                Exploration = exploration,
                CuriosityQuestion = cq
            };
        }

        public Dictionary<string, object> UpdateRelationship(string targetId, string eventType)
        {
            if (!Modules.Relationship)
                return new Dictionary<string, object> { { "skipped", true }, { "reason", SoulCoreLocalization.Rt("rt.soul.relOff") } };
            return Relationship.ApplyEvent(Id, targetId, eventType, new Dictionary<string, float>(Personality.Traits));
        }

        public int GetRelation(string targetId) => Relationship.GetValue(Id, targetId);
        public string GetRelationLevel(string targetId) => Relationship.GetLevelName(Id, targetId);

        public void LearnFeedback(bool success, string topic = "")
        {
            if (string.IsNullOrEmpty(topic)) return;
            if (success)
            {
                if (Modules.Interest) Interest.Update(topic, 0.1f);
                if (Modules.Consistency) Consistency.RecordSuccess(topic);
            }
            else
            {
                if (Modules.Interest) Interest.Update(topic, -0.05f);
                if (Modules.Consistency) Consistency.RecordFailure(topic);
            }
        }

        public void Shutdown()
        {
            _running = false;
            Memory.Forget();
        }

        public bool IsRunning() => _running;

        public Dictionary<string, object> GetProfile()
        {
            return new Dictionary<string, object>
            {
                { "id", Id },
                { "name", Name },
                { "traits", Personality.Traits },
                { "emotions", Emotion.Emotions },
                { "dominant_emotion", Emotion.GetDominant() },
                { "stats", Stats },
                { "memory_count", Memory.Stats["total"] },
                { "relationship_count", Relationship.CountForAgent(Id) },
                { "interests", Interest.GetTopInterests(5) },
                { "curiosity_level", Curiosity.CuriosityLevel },
                { "consistency_score", Consistency.GetConsistencyScore() },
                { "preferences", Preference.Preferences }
            };
        }

        public string Explain()
        {
            var sb = new StringBuilder();
            sb.AppendLine(SoulCoreLocalization.RtFormat("rt.explain.title", Name));
            sb.AppendLine(SoulCoreLocalization.Rt("rt.explain.personality"));
            var traits = Personality.Traits.OrderBy(x => x.Key).Take(10);
            foreach (var kv in traits)
            {
                var n = (int)(kv.Value * 10);
                sb.AppendLine($"  {kv.Key,-12} {new string('█', n)}{new string('░', 10 - n)} {kv.Value:F2}");
            }
            sb.AppendLine();
            sb.AppendLine(SoulCoreLocalization.Rt("rt.explain.emotion"));
            var dom = Emotion.GetDominant();
            foreach (var kv in Emotion.Emotions.Where(x => x.Value > 0.3f).OrderByDescending(x => x.Value))
            {
                var star = kv.Key == dom ? "★" : "·";
                sb.AppendLine($"  {star} {kv.Key,-12} {kv.Value:F2}");
            }
            return sb.ToString();
        }

        public void DailyReset()
        {
            var M = Modules;
            if (M.Emotion) Emotion.Decay(24f * EmotionDailyDecayScale);
            if (M.Interest) Interest.Decay();
            if (M.Curiosity) Curiosity.UpdateCuriosity(0.05f);
            if (M.Dream)
            {
                var d = Dream.Generate();
                if (M.Memory && !string.IsNullOrEmpty(d)) Memory.StoreForced(SoulCoreLocalization.RtFormat("rt.soul.dreamMem", d), 4);
            }
            if (M.Relationship) Relationship.DailyUpdate(_rng);
            if (M.Memory) Memory.Forget();
        }

        public SoulSaveData ExportSnapshot()
        {
            if (!SoulCoreEdition.CanUseSnapshot)
            {
                SoulFreeEditionUi.Notify(SoulFreeEditionNotifyKind.SnapshotNotAvailable);
                return null;
            }
            var s = new SoulSaveData
            {
                Id = Id,
                Name = Name,
                ExperienceCount = Stats["experience_count"],
                DecisionCount = Stats["decision_count"],
                DailyInteractions = Stats["daily_interactions"]
            };
            foreach (var kv in Personality.Traits)
                s.Traits.Add(new SerializableTrait { Key = kv.Key, Value = kv.Value });
            foreach (var kv in Emotion.Emotions)
                s.Emotions.Add(new SerializableEmotion { Key = kv.Key, Value = kv.Value });
            return s;
        }

        public void ApplySnapshot(SoulSaveData data)
        {
            if (!SoulCoreEdition.CanUseSnapshot)
            {
                SoulFreeEditionUi.Notify(SoulFreeEditionNotifyKind.SnapshotNotAvailable);
                return;
            }
            if (data == null) return;
            if (!string.IsNullOrEmpty(data.Name)) { /* 显示名由外部组件维护 */ }
            Stats["experience_count"] = data.ExperienceCount;
            Stats["decision_count"] = data.DecisionCount;
            Stats["daily_interactions"] = data.DailyInteractions;
            foreach (var t in data.Traits)
            {
                if (Personality.Traits.ContainsKey(t.Key)) Personality.Traits[t.Key] = t.Value;
            }
            foreach (var e in data.Emotions)
            {
                if (Emotion.Emotions.ContainsKey(e.Key)) Emotion.Emotions[e.Key] = e.Value;
            }
        }
    }
}
