using System;
using System.Collections.Generic;

namespace SoulCore
{
    [Serializable]
    public struct PerceptionContext
    {
        public string EventType;
        public string Content;
        public float Intensity;
        public string TargetId;
        public string UserEmotion;
        public Dictionary<string, string> Metadata;

        public PerceptionContext(
            string eventType,
            string content = "",
            float intensity = 0.5f,
            string targetId = null,
            string userEmotion = null,
            Dictionary<string, string> metadata = null)
        {
            EventType = eventType;
            Content = content ?? string.Empty;
            Intensity = intensity;
            TargetId = targetId;
            UserEmotion = userEmotion;
            Metadata = metadata ?? new Dictionary<string, string>();
        }
    }

    [Serializable]
    public class SoulDecision
    {
        public string Action;
        public string Explanation;
        public float Confidence;
        public string Emotion;
        public float EmotionIntensity;
        public List<Memory> RelevantMemories;
        public string Intuition;
        public string Inspiration;
        public string Dream;
        public string Serendipity;
        public string Humor;
        public string Complaint;
        public string Embarrassment;
        public string Nostalgia;
        public string Awe;
        public string Exploration;
        public string CuriosityQuestion;
    }
}
