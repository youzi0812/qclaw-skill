using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SoulCore.Advanced;

namespace SoulCore.Tests
{
    /// <summary>契约与静态策略的回归测试（Edit Mode，无需进 Play）。</summary>
    public sealed class AdvancedPipelineContractTests
    {
        [Test]
        public void AdvancedSignalKeys_AllPublicStringConstants_AreUniqueAndNonEmpty()
        {
            var t = typeof(AdvancedSignalKeys);
            var strings = t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.FieldType == typeof(string))
                .Select(f => (string)f.GetValue(null))
                .ToList();
            Assert.That(strings, Is.Not.Empty);
            foreach (var s in strings)
            {
                Assert.That(s, Is.Not.Null.And.Not.Empty);
            }

            Assert.That(strings.Distinct().Count(), Is.EqualTo(strings.Count));
        }

        [Test]
        public void AdvancedDriveKeys_AllPublicStringConstants_AreUniqueAndNonEmpty()
        {
            var t = typeof(AdvancedDriveKeys);
            var strings = t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.FieldType == typeof(string))
                .Select(f => (string)f.GetValue(null))
                .ToList();
            Assert.That(strings.Count, Is.EqualTo(4));
            Assert.That(strings.Distinct().Count(), Is.EqualTo(4));
        }

        [Test]
        public void AdvancedSignalIdTable_KeysMatchEnumCardinality_AndAreUnique()
        {
            var constCount = typeof(AdvancedSignalKeys).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Count(f => f.FieldType == typeof(string));
            var enumCount = typeof(AdvancedSignalId).GetEnumValues().Length;
            Assert.That(enumCount, Is.EqualTo(constCount), "新增 Signal 常量时请同步 AdvancedSignalId 与 AdvancedSignalIdTable。");

            var keys = typeof(AdvancedSignalId).GetEnumValues()
                .Cast<AdvancedSignalId>()
                .Select(AdvancedSignalIdTable.Key)
                .ToList();
            Assert.That(keys.Distinct().Count(), Is.EqualTo(keys.Count));
        }

        [Test]
        public void AdvancedDriveIdTable_KeysMatchEnumCardinality_AndAreUnique()
        {
            var constCount = typeof(AdvancedDriveKeys).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Count(f => f.FieldType == typeof(string));
            var enumCount = typeof(AdvancedDriveId).GetEnumValues().Length;
            Assert.That(enumCount, Is.EqualTo(constCount));

            var keys = typeof(AdvancedDriveId).GetEnumValues()
                .Cast<AdvancedDriveId>()
                .Select(AdvancedDriveIdTable.Key)
                .ToList();
            Assert.That(keys.Distinct().Count(), Is.EqualTo(keys.Count));
        }

        [Test]
        public void OpenBehaviorPerceptionMetadataKeys_SignalSnapshot_PrefixesPipelineKey()
        {
            var k = AdvancedSignalIdTable.Key(AdvancedSignalId.FePressure);
            var meta = OpenBehaviorPerceptionMetadataKeys.SignalSnapshot(k);
            Assert.That(meta, Does.StartWith("signal_").And.Contain("fe_pressure"));
        }

        [Test]
        public void PriorityPolicy9_Evaluate_NullNpc_ReturnsNone()
        {
            var policy = new PriorityPolicy9();
            var state = new AdvancedNpcState { Npc = null };
            var intent = policy.Evaluate(state, 0.55f, 0.62f, 0.56f, 0.58f);
            Assert.That(intent.Kind, Is.EqualTo(AdvancedIntentKind.None));
        }
    }
}
