using System;
using System.Collections.Generic;
using System.Linq;

namespace SoulCore
{
    public class EmotionEngine
    {
        public readonly Dictionary<string, float> Emotions = new Dictionary<string, float>
        {
            { "joy", 0.5f },
            { "sadness", 0.2f },
            { "fear", 0.1f },
            { "anger", 0.1f },
            { "surprise", 0.1f },
            { "disgust", 0.1f },
            { "missing", 0.3f },
            { "loneliness", 0.3f },
            { "anxiety", 0.2f },
            { "hope", 0.5f },
            { "despair", 0.1f },
            { "gratitude", 0.3f },
            { "curiosity_emotion", 0.4f },
            { "pride", 0.2f },
            { "shame", 0.1f }
        };

        private readonly Dictionary<string, float> _decayRates = new()
        {
            { "joy", 0.02f }, { "fear", 0.03f }, { "anger", 0.02f },
            { "sadness", 0.01f }, { "surprise", 0.05f }, { "anxiety", 0.02f },
            { "curiosity_emotion", 0.01f }, { "pride", 0.01f }, { "shame", 0.02f }
        };

        public void Update(
            string eventType,
            float intensity,
            string content = "",
            Dictionary<string, float> personality = null)
        {
            var changes = new Dictionary<string, float>();

            switch (eventType)
            {
                case "disaster":
                    changes["fear"] = intensity * 0.3f;
                    changes["anxiety"] = 0.1f;
                    changes["hope"] = -0.1f;
                    break;
                case "help_others":
                    changes["joy"] = 0.2f;
                    changes["hope"] = 0.1f;
                    changes["gratitude"] = 0.1f;
                    break;
                case "insult":
                    changes["anger"] = 0.2f;
                    changes["sadness"] = 0.1f;
                    break;
                case "gift":
                    changes["joy"] = 0.2f;
                    changes["gratitude"] = 0.2f;
                    break;
                case "betray":
                    changes["sadness"] = 0.3f;
                    changes["anger"] = 0.2f;
                    changes["despair"] = 0.1f;
                    break;
                case "rescued":
                    changes["joy"] = 0.4f;
                    changes["hope"] = 0.2f;
                    changes["gratitude"] = 0.3f;
                    break;
                case "learn":
                    changes["curiosity_emotion"] = 0.2f;
                    changes["joy"] = 0.1f;
                    break;
                case "success":
                    changes["pride"] = 0.3f;
                    changes["joy"] = 0.2f;
                    break;
                case "failure":
                    changes["shame"] = 0.2f;
                    changes["sadness"] = 0.1f;
                    break;
                case "conversation" when !string.IsNullOrEmpty(content):
                    if (content.Contains("谢谢") || content.Contains("感谢"))
                    { changes["gratitude"] = 0.2f; changes["joy"] = 0.1f; }
                    else if (content.Contains("对不起") || content.Contains("抱歉"))
                        changes["shame"] = 0.1f;
                    else if (content.Contains("想") && content.Contains("念"))
                        changes["missing"] = 0.1f;
                    else if (content.Contains("厉害") || content.Contains("佩服"))
                    { changes["pride"] = 0.1f; changes["joy"] = 0.1f; }
                    break;
            }

            foreach (var key in changes.Keys.ToList())
                changes[key] *= intensity;

            if (personality != null)
            {
                float p(string k, float d) => personality.TryGetValue(k, out var v) ? v : d;
                if (p("optimism", 0.5f) > 0.7f)
                    changes["hope"] = (changes.ContainsKey("hope") ? changes["hope"] : 0) + 0.1f;
                if (p("fear_tendency", 0.2f) > 0.6f)
                    changes["fear"] = (changes.ContainsKey("fear") ? changes["fear"] : 0) * 1.3f;
                if (p("anger_tendency", 0.2f) > 0.6f)
                    changes["anger"] = (changes.ContainsKey("anger") ? changes["anger"] : 0) * 1.2f;
                if (p("hope_tendency", 0.6f) > 0.7f)
                    changes["hope"] = (changes.ContainsKey("hope") ? changes["hope"] : 0) + 0.1f;
            }

            foreach (var kv in changes)
            {
                if (Emotions.ContainsKey(kv.Key))
                    Emotions[kv.Key] = Math.Clamp(Emotions[kv.Key] + kv.Value, 0f, 1f);
            }

            _normalize();
        }

        public void AddEmotion(string emotion, float delta)
        {
            if (Emotions.ContainsKey(emotion))
                Emotions[emotion] = Math.Clamp(Emotions[emotion] + delta, 0f, 1f);
        }

        public void Decay(float hoursPassed)
        {
            var factor = hoursPassed / 24f;
            foreach (var k in _decayRates.Keys)
            {
                if (!Emotions.ContainsKey(k)) continue;
                var decay = _decayRates[k] * factor;
                Emotions[k] = Math.Max(0, Emotions[k] - decay);
            }

            foreach (var e in new[] { "missing", "loneliness", "gratitude", "hope", "pride", "shame" })
            {
                if (Emotions.ContainsKey(e))
                    Emotions[e] = Math.Max(0, Emotions[e] - 0.02f * factor);
            }

            _normalize();
        }

        public string GetDominant() => Emotions.OrderByDescending(x => x.Value).First().Key;

        public float GetIntensity() => Emotions.Values.Sum() / Emotions.Count;

        private void _normalize()
        {
            foreach (var k in Emotions.Keys.ToList())
                Emotions[k] = Math.Clamp(Emotions[k], 0f, 1f);
        }
    }
}
