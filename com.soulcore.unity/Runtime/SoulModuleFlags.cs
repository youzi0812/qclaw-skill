using System;

namespace SoulCore
{
    [Serializable]
    public struct SoulModuleFlags
    {
        public bool Emotion;
        public bool Memory;
        public bool Decision;
        public bool Relationship;
        public bool Interest;
        public bool Curiosity;
        public bool Habit;
        public bool Serendipity;
        public bool EmotionInfect;
        public bool Humor;
        public bool Complain;
        public bool Embarrassment;
        public bool Exploration;
        public bool Preference;
        public bool Nostalgia;
        public bool Awe;
        public bool Intuition;
        public bool Inspiration;
        public bool Dream;
        public bool Consistency;

        public static SoulModuleFlags AllEnabled => new SoulModuleFlags
        {
            Emotion = true, Memory = true, Decision = true, Relationship = true,
            Interest = true, Curiosity = true, Habit = true, Serendipity = true,
            EmotionInfect = true, Humor = true, Complain = true, Embarrassment = true,
            Exploration = true, Preference = true, Nostalgia = true, Awe = true,
            Intuition = true, Inspiration = true, Dream = true, Consistency = true
        };
    }
}
