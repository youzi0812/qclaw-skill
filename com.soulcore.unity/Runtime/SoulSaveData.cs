using System;
using System.Collections.Generic;

namespace SoulCore
{
    [Serializable]
    public class SoulSaveData
    {
        public string Id;
        public string Name;
        public int ExperienceCount;
        public int DecisionCount;
        public int DailyInteractions;
        public List<SerializableTrait> Traits = new();
        public List<SerializableEmotion> Emotions = new();
    }

    [Serializable]
    public struct SerializableTrait
    {
        public string Key;
        public float Value;
    }

    [Serializable]
    public struct SerializableEmotion
    {
        public string Key;
        public float Value;
    }
}
