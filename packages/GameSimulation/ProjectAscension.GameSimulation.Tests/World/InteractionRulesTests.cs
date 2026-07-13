using System.Collections.Generic;
using ProjectAscension.GameSimulation.World;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.World
{
    /// <summary>
    /// Picking which interactable a press of [F] should hit: nearest wins, but only among candidates
    /// within THEIR OWN reach — a board must not steal the prompt from across the map just because
    /// nothing closer happens to be in range too. Headless (ADR: Unity is a shell) so the selection
    /// itself is verified without Unity.
    /// </summary>
    public class InteractionRulesTests
    {
        [Fact]
        public void EmptyList_ReturnsNone()
        {
            Assert.Equal(-1, InteractionRules.Best(new List<InteractCandidate>()));
        }

        [Fact]
        public void NoCandidateInReach_ReturnsNone()
        {
            var candidates = new List<InteractCandidate>
            {
                new(Id: 1, Distance: 5f, Reach: 3f),
                new(Id: 2, Distance: 10f, Reach: 4f),
            };
            Assert.Equal(-1, InteractionRules.Best(candidates));
        }

        [Fact]
        public void NearestInReach_Wins()
        {
            var candidates = new List<InteractCandidate>
            {
                new(Id: 1, Distance: 2f, Reach: 5f),
                new(Id: 2, Distance: 0.5f, Reach: 5f),
                new(Id: 3, Distance: 3f, Reach: 5f),
            };
            Assert.Equal(2, InteractionRules.Best(candidates));
        }

        [Fact]
        public void PerCandidateReach_IsRespected()
        {
            // Id 1 is nearer but out of ITS OWN reach; Id 2 is farther but within its (larger) reach —
            // a board being readable from further away than a lootable is the whole point of a
            // per-candidate reach instead of one global radius.
            var candidates = new List<InteractCandidate>
            {
                new(Id: 1, Distance: 4f, Reach: 2f),
                new(Id: 2, Distance: 6f, Reach: 8f),
            };
            Assert.Equal(2, InteractionRules.Best(candidates));
        }

        [Fact]
        public void TiedDistance_LowestIdWins_Deterministically()
        {
            var candidates = new List<InteractCandidate>
            {
                new(Id: 5, Distance: 2f, Reach: 5f),
                new(Id: 2, Distance: 2f, Reach: 5f),
                new(Id: 9, Distance: 2f, Reach: 5f),
            };
            Assert.Equal(2, InteractionRules.Best(candidates));
        }
    }
}
