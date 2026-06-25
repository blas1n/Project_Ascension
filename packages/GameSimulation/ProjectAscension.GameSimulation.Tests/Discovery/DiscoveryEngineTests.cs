using System.Collections.Generic;
using ProjectAscension.GameSimulation.Discovery;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Discovery
{
    public class DiscoveryEngineTests
    {
        private static ISet<string> Context(params string[] tags) => new HashSet<string>(tags);

        private static void Repeat(DiscoveryState state, BehaviorKind behavior, ISet<string> context, int times)
        {
            var observation = new Observation(behavior, context);
            for (int i = 0; i < times; i++)
                DiscoveryEngine.Apply(state, DiscoveryCatalog.All, observation);
        }

        [Fact]
        public void SameContext_DifferentBehavior_YieldsDifferentDiscovery()
        {
            var ranged = new DiscoveryState();
            Repeat(ranged, BehaviorKind.RangedAttack, Context("arcane"), 20);
            Assert.True(ranged.IsDiscovered("flame_bullet"));
            Assert.False(ranged.IsDiscovered("flame_lance"));
            Assert.False(ranged.IsDiscovered("thermal_barrier"));

            var melee = new DiscoveryState();
            Repeat(melee, BehaviorKind.MeleeAttack, Context("arcane", "melee"), 20);
            Assert.True(melee.IsDiscovered("flame_lance"));
            Assert.False(melee.IsDiscovered("flame_bullet"));

            var defensive = new DiscoveryState();
            Repeat(defensive, BehaviorKind.Dodge, Context("arcane"), 20);
            Assert.True(defensive.IsDiscovered("thermal_barrier"));
            Assert.False(defensive.IsDiscovered("flame_bullet"));
        }

        [Fact]
        public void Context_IsRequired()
        {
            var state = new DiscoveryState();
            Repeat(state, BehaviorKind.RangedAttack, Context(), 30); // no "arcane"/"firearm"
            Assert.False(state.IsDiscovered("flame_bullet"));
            Assert.False(state.IsDiscovered("rapid_fire"));
        }

        [Fact]
        public void Prerequisite_GatesGraphDiscovery()
        {
            var state = new DiscoveryState();
            Repeat(state, BehaviorKind.Jump, Context(), 40);
            Assert.True(state.IsDiscovered("double_jump"));   // unlocks at 30
            Assert.False(state.IsDiscovered("high_jump"));    // only ~10 progress after prereq

            Repeat(state, BehaviorKind.Jump, Context(), 40);
            Assert.True(state.IsDiscovered("high_jump"));
        }

        [Fact]
        public void Apply_ReturnsNewlyUnlocked_Once()
        {
            var state = new DiscoveryState();
            Repeat(state, BehaviorKind.DodgeAttack, Context(), 14);
            var unlocked = DiscoveryEngine.Apply(state, DiscoveryCatalog.All,
                new Observation(BehaviorKind.DodgeAttack, Context()));
            Assert.Contains(unlocked, c => c.Key == "dodge_slash");

            var again = DiscoveryEngine.Apply(state, DiscoveryCatalog.All,
                new Observation(BehaviorKind.DodgeAttack, Context()));
            Assert.DoesNotContain(again, c => c.Key == "dodge_slash");
        }
    }
}
