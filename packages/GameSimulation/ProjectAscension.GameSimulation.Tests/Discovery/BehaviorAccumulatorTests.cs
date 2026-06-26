using System.Linq;
using ProjectAscension.GameSimulation.Discovery;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Discovery
{
    public class BehaviorAccumulatorTests
    {
        [Fact]
        public void Record_CountsPerBehavior()
        {
            var acc = new BehaviorAccumulator();
            acc.Record(BehaviorKind.Jump);
            acc.Record(BehaviorKind.Jump);
            acc.Record(BehaviorKind.MeleeAttack);

            Assert.Equal(2, acc.Counts["Jump"]);
            Assert.Equal(1, acc.Counts["MeleeAttack"]);
            Assert.True(acc.HasActivity);
        }

        [Fact]
        public void SetContext_DeduplicatesAndDropsBlanks()
        {
            var acc = new BehaviorAccumulator();
            acc.SetContext(new[] { "arcane", "arcane", " ", "fire" });

            Assert.Equal(new[] { "arcane", "fire" }, acc.Tags.OrderBy(t => t).ToArray());
        }

        [Fact]
        public void Reset_ClearsCountsButKeepsContext()
        {
            var acc = new BehaviorAccumulator();
            acc.SetContext(new[] { "arcane" });
            acc.Record(BehaviorKind.Jump);

            acc.Reset();

            Assert.False(acc.HasActivity);
            Assert.Empty(acc.Counts);
            Assert.Contains("arcane", acc.Tags); // context persists across flushes
        }
    }
}
