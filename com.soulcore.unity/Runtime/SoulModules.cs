using System;
using System.Collections.Generic;
using System.Linq;

namespace SoulCore
{
    public class InterestSystem
    {
        private readonly Dictionary<string, float> _topics = new();
        public void Update(string topic, float intensity = 0.05f)
        {
            if (string.IsNullOrEmpty(topic)) return;
            var key = topic.Length > 40 ? topic.Substring(0, 40) : topic;
            _topics[key] = Math.Clamp((_topics.TryGetValue(key, out var v) ? v : 0) + intensity, 0f, 1f);
        }
        public void Decay()
        {
            foreach (var k in _topics.Keys.ToList())
                _topics[k] = Math.Max(0, _topics[k] - 0.02f);
        }
        public List<(string topic, float value)> GetTopInterests(int n) =>
            _topics.OrderByDescending(x => x.Value).Take(n).Select(x => (x.Key, x.Value)).ToList();
    }

    public class CuriositySystem
    {
        public float CuriosityLevel = 0.5f;
        public void UpdateCuriosity(float delta) => CuriosityLevel = Math.Clamp(CuriosityLevel + delta, 0f, 1f);
        public string GenerateQuestion() =>
            CuriosityLevel > 0.4f ? "你为什么会来这里？" : null;
    }

    public class HabitSystem
    {
        public HabitSystem(Soul _) { }
        public List<Dictionary<string, object>> GetAllHabits() => new();
        public string CheckAndTrigger(string time) => null;
        public void IncrementStrength(string name, float amount) { }
    }

    public class SerendipitySystem
    {
        private int _count;
        public SerendipitySystem(Soul _) { }
        public string Check(string _)
        {
            if (new System.Random().NextDouble() < 0.05)
            { _count++; return "今天有种奇妙的巧合感"; }
            return null;
        }
        public int GetCoincidenceCount() => _count;
    }

    public class EmotionInfectionSystem
    {
        private readonly Soul _s;
        public EmotionInfectionSystem(Soul soul) { _s = soul; }
        public void Infect(string userEmotion, float intensity)
        {
            if (string.IsNullOrEmpty(userEmotion) || _s == null) return;
            if (_s.Emotion.Emotions.ContainsKey(userEmotion))
                _s.Emotion.AddEmotion(userEmotion, intensity * 0.1f);
        }
    }

    public class HumorSystem
    {
        private readonly Soul _s;
        public HumorSystem(Soul s) { _s = s; }
        public string TellJoke(IReadOnlyDictionary<string, string> situation) =>
            _s.Personality.GetTrait("optimism", 0.5f) > 0.6f && new System.Random().NextDouble() < 0.2
                ? "哈哈，这剧情比我想的还离谱" : null;
    }

    public class ComplainSystem
    {
        private readonly Soul _s;
        public ComplainSystem(Soul s) { _s = s; }
        public string Complain(string eventType) =>
            _s.Personality.GetTrait("anger_tendency", 0.2f) > 0.5f && new System.Random().NextDouble() < 0.15
                ? "真是够了……" : null;
    }

    public class EmbarrassmentSystem
    {
        public EmbarrassmentSystem(Soul _) { }
        public string Trigger(IReadOnlyDictionary<string, string> situation) =>
            new System.Random().NextDouble() < 0.1 ? "……别盯着我看。" : null;
    }

    public class ExplorationSystem
    {
        public ExplorationSystem(Soul _, InterestSystem __) { }
        public string WantToExplore() => "想去那边看看。";
    }

    public class PreferenceSystem
    {
        public readonly Dictionary<string, float> Preferences = new();
    }

    public class NostalgiaSystem
    {
        public NostalgiaSystem(Soul _) { }
        public string Trigger() => new System.Random().NextDouble() < 0.1 ? "突然想起以前的事……" : null;
    }

    public class AweSystem
    {
        public AweSystem(Soul _) { }
        public string ExpressAwe(string content) =>
            !string.IsNullOrEmpty(content) && (content.Contains("美") || content.Contains("光"))
                ? "真漂亮……" : null;
    }

    public class IntuitionSystemSimple
    {
        public IntuitionSystemSimple(Soul _) { }
        public string Generate(IReadOnlyDictionary<string, string> situation) =>
            new System.Random().NextDouble() < 0.12 ? "心里莫名有点不安" : null;
        public float GetAccuracy() => 0.5f;
    }

    public class InspirationSystemSimple
    {
        public InspirationSystemSimple(Soul _) { }
        public string Generate(IReadOnlyDictionary<string, string> situation) =>
            new System.Random().NextDouble() < 0.1 ? "有了，一个新点子" : null;
    }

    public class DreamSystemSimple
    {
        public readonly List<string> Dreams = new();
        public string Generate()
        {
            if (new System.Random().NextDouble() < 0.3)
            { var d = "梦见在追一束光"; Dreams.Add(d); return d; }
            return null;
        }
    }

    public class ConsistencySystem
    {
        public ConsistencySystem(Soul _) { }
        public void Update(IReadOnlyDictionary<string, string> situation, string action) { }
        public void RecordSuccess(string topic) { }
        public void RecordFailure(string topic) { }
        public float GetConsistencyScore() => 0.75f;
    }
}
