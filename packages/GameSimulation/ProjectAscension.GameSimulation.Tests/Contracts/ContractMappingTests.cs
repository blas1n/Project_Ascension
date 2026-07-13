using ProjectAscension.Domain.Enums;
using ProjectAscension.GameSimulation.Contracts;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Contracts
{
    /// <summary>
    /// The server-contract → runtime mapping, now headless (ADR: Unity is a shell). These were
    /// previously object-initializer decisions buried in CityHub/GameSession MonoBehaviours and
    /// invisible to sims — the purpose parse with a safe default, the target-count floor, and the
    /// null defaults. A bad DTO from the server must never crash the city loop.
    /// </summary>
    public class ContractMappingTests
    {
        private static ContractInstance Map(
            string? purpose = "Hunt", string? title = "t", string? description = "d", int targetCount = 3,
            int rewardCurrency = 10, string target = "elite", string? issuer = "office",
            bool delegationAllowed = false, int rewardReputation = 0, int minReputation = 0,
            int timeLimitSeconds = 0, bool failOnTimeout = false, bool failOnDeath = false)
            => ContractMapping.FromFields(purpose, title, description, targetCount, rewardCurrency,
                target, issuer, delegationAllowed, rewardReputation, minReputation, timeLimitSeconds,
                failOnTimeout, failOnDeath);

        [Theory]
        [InlineData("Hunt", ContractPurpose.Hunt)]
        [InlineData("Survey", ContractPurpose.Survey)]
        [InlineData("Collection", ContractPurpose.Collection)]
        public void Purpose_ParsesTheKnownPurposes(string raw, ContractPurpose expected)
        {
            Assert.Equal(expected, Map(purpose: raw).Purpose);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("nonsense")]
        public void Purpose_FallsBackToHuntOnAnUnknownValue(string? raw)
        {
            Assert.Equal(ContractPurpose.Hunt, Map(purpose: raw).Purpose);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-5, 1)]
        [InlineData(1, 1)]
        [InlineData(7, 7)]
        public void TargetCount_HasAFloorOfOne(int given, int expected)
        {
            // A contract with a 0 target would be complete on accept — always needs ≥1 to clear.
            Assert.Equal(expected, Map(targetCount: given).TargetCount);
        }

        [Fact]
        public void NullStrings_BecomeEmptyNotNull()
        {
            var c = Map(title: null, description: null, issuer: null);
            Assert.Equal("", c.Title);
            Assert.Equal("", c.Description);
            Assert.Equal("", c.Issuer);
        }

        [Fact]
        public void ItemReward_IsCarried_AndAbsenceIsHarmless()
        {
            // The first hour's survey pays in a map, not gold.
            var withMap = ContractMapping.FromFields("Survey", "Map the Frontier", "d", 1, 20, null, "office",
                false, 4, 0, 0, false, false, rewardItemKey: "frontier_map", rewardItemAmount: 1);
            Assert.Equal("frontier_map", withMap.RewardItemKey);
            Assert.Equal(1, withMap.RewardItemAmount);

            // A contract with no item reward carries an empty key, never a phantom item.
            var goldOnly = Map();
            Assert.Equal("", goldOnly.RewardItemKey);
            Assert.Equal(0, goldOnly.RewardItemAmount);
        }

        [Fact]
        public void CarriesTheFullTerms()
        {
            var c = Map(rewardReputation: 5, minReputation: 10, timeLimitSeconds: 120,
                failOnTimeout: true, failOnDeath: true, delegationAllowed: true, target: "wraith");
            Assert.Equal(5, c.RewardReputation);
            Assert.Equal(10, c.MinReputation);
            Assert.Equal(120, c.TimeLimitSeconds);
            Assert.True(c.FailOnTimeout);
            Assert.True(c.FailOnDeath);
            Assert.True(c.DelegationAllowed);
            Assert.Equal("wraith", c.Target);
        }
    }
}
