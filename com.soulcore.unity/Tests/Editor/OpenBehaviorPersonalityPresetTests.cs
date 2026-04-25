using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace SoulCore.Tests
{
    /// <summary>OpenBehavior 人格预设的回归测试（Edit Mode）。</summary>
    public sealed class OpenBehaviorPersonalityPresetTests
    {
        private static readonly BindingFlags _instanceFlags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        [Test]
        public void ApplyPersonalityPreset_MapsFieldsAndClampsRanges()
        {
            var go = new GameObject("OpenBehaviorPresetTest");
            var driver = go.AddComponent<OpenBehaviorDriver>();
            var preset = ScriptableObject.CreateInstance<OpenBehaviorPersonalityPreset>();
            try
            {
                preset.personalityAffectsSocialExpression = false;
                preset.personalityAffectsTargetSelection = false;
                preset.personalityTargetSelectionWeight = 1.6f; // clamp -> 1
                preset.personalityAffectsStylePrefix = false;
                preset.personalityStyleWeight = -0.2f; // clamp -> 0
                preset.personalityAffectsTopicPreference = false;
                preset.personalityTopicWeight = 0.35f;

                preset.taskPeriodPersonalityWeightOverride = true;
                preset.taskPeriodTargetSelectionWeightMultiplier = -2f; // clamp -> 0
                preset.taskPeriodStyleWeightMultiplier = 2.6f; // clamp -> 2
                preset.taskPeriodTopicWeightMultiplier = 1.2f;

                preset.urgentTaskExtraPersonalityWeightOverride = true;
                preset.urgentTaskTargetSelectionWeightMultiplier = -1f; // clamp -> 0
                preset.urgentTaskStyleWeightMultiplier = 3f; // clamp -> 2
                preset.urgentTaskTopicWeightMultiplier = 0.5f;

                driver.ApplyPersonalityPreset(preset);

                Assert.That(_getField<bool>(driver, "_personalityAffectsSocialExpression"), Is.False);
                Assert.That(_getField<bool>(driver, "_personalityAffectsTargetSelection"), Is.False);
                Assert.That(_getField<float>(driver, "_personalityTargetSelectionWeight"), Is.EqualTo(1f).Within(1e-6f));
                Assert.That(_getField<bool>(driver, "_personalityAffectsStylePrefix"), Is.False);
                Assert.That(_getField<float>(driver, "_personalityStyleWeight"), Is.EqualTo(0f).Within(1e-6f));
                Assert.That(_getField<bool>(driver, "_personalityAffectsTopicPreference"), Is.False);
                Assert.That(_getField<float>(driver, "_personalityTopicWeight"), Is.EqualTo(0.35f).Within(1e-6f));

                Assert.That(_getField<bool>(driver, "_taskPeriodPersonalityWeightOverride"), Is.True);
                Assert.That(_getField<float>(driver, "_taskPeriodTargetSelectionWeightMultiplier"), Is.EqualTo(0f).Within(1e-6f));
                Assert.That(_getField<float>(driver, "_taskPeriodStyleWeightMultiplier"), Is.EqualTo(2f).Within(1e-6f));
                Assert.That(_getField<float>(driver, "_taskPeriodTopicWeightMultiplier"), Is.EqualTo(1.2f).Within(1e-6f));

                Assert.That(_getField<bool>(driver, "_urgentTaskExtraPersonalityWeightOverride"), Is.True);
                Assert.That(_getField<float>(driver, "_urgentTaskTargetSelectionWeightMultiplier"), Is.EqualTo(0f).Within(1e-6f));
                Assert.That(_getField<float>(driver, "_urgentTaskStyleWeightMultiplier"), Is.EqualTo(2f).Within(1e-6f));
                Assert.That(_getField<float>(driver, "_urgentTaskTopicWeightMultiplier"), Is.EqualTo(0.5f).Within(1e-6f));
            }
            finally
            {
                Object.DestroyImmediate(preset);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ApplyPersonalityPresetFromReference_AppliesReferencedPreset()
        {
            var go = new GameObject("OpenBehaviorPresetRefTest");
            var driver = go.AddComponent<OpenBehaviorDriver>();
            var preset = ScriptableObject.CreateInstance<OpenBehaviorPersonalityPreset>();
            try
            {
                preset.personalityTargetSelectionWeight = 0.77f;
                _setField(driver, "_personalityPreset", preset);

                driver.ApplyPersonalityPresetFromReference();

                Assert.That(_getField<float>(driver, "_personalityTargetSelectionWeight"), Is.EqualTo(0.77f).Within(1e-6f));
            }
            finally
            {
                Object.DestroyImmediate(preset);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OpenBehaviorPersonalityPreset_Defaults_AreStable()
        {
            var preset = ScriptableObject.CreateInstance<OpenBehaviorPersonalityPreset>();
            try
            {
                Assert.That(preset.personalityAffectsSocialExpression, Is.True);
                Assert.That(preset.personalityAffectsTargetSelection, Is.True);
                Assert.That(preset.personalityTargetSelectionWeight, Is.EqualTo(0.5f).Within(1e-6f));
                Assert.That(preset.personalityStyleWeight, Is.EqualTo(0.6f).Within(1e-6f));
                Assert.That(preset.personalityTopicWeight, Is.EqualTo(0.6f).Within(1e-6f));
                Assert.That(preset.urgentTaskExtraPersonalityWeightOverride, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(preset);
            }
        }

        private static T _getField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, _instanceFlags);
            Assert.That(field, Is.Not.Null, $"未找到字段: {fieldName}");
            return (T)field.GetValue(target);
        }

        private static void _setField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, _instanceFlags);
            Assert.That(field, Is.Not.Null, $"未找到字段: {fieldName}");
            field.SetValue(target, value);
        }
    }
}
