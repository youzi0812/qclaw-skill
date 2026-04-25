using System;
using System.Collections.Generic;

namespace SoulCore
{
    [Serializable]
    public class Memory
    {
        public string Id;
        public string Content;
        public string Type;
        public int Importance;
        public double CreatedAt;
        public double? LastAccessed;
        public int AccessCount;
        public float Strength;
        public string Emotion;
        public List<string> Associations;

        public void Strengthen(double now)
        {
            AccessCount++;
            LastAccessed = now;
            Strength = Math.Min(1f, Strength + 0.1f);
        }

        public void Decay(double daysPassed)
        {
            if (Importance >= 9) return;
            var decayRate = 0.05f * (10 - Importance) / 9f;
            Strength = Math.Max(0f, Strength * (1f - (float)decayRate * (float)daysPassed));
        }

        public bool IsForgotten() => Strength < 0.1f;

        internal void PrepareReuse()
        {
            Id = null;
            Content = null;
            Type = "event";
            Importance = 5;
            CreatedAt = 0;
            LastAccessed = null;
            AccessCount = 0;
            Strength = 1f;
            Emotion = null;
            if (Associations == null) Associations = new List<string>(4);
            else Associations.Clear();
        }
    }
}
