namespace SoulCore
{
    public interface ISoulState
    {
        string Id { get; }
        string Name { get; }
        PersonalityEngine Personality { get; }
        EmotionEngine Emotion { get; }
        float GetResourceScarcity();
    }
}
