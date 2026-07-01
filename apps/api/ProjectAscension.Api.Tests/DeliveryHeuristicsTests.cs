using System.Linq;
using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;
using Xunit;

namespace ProjectAscension.Api.Tests;

// The delivery fallback derives from play on the same grid the prompt guides the LLM with
// (attack decides beam/projectile; mobility the mobile variant). These lock that mapping.
public class DeliveryHeuristicsTests
{
    private static BehaviorCount B(string behavior, int count) => new(behavior, count);

    [Fact]
    public void ChargedStanding_IsBeam_ChargedMobile_IsNova()
    {
        Assert.Equal("beam", DeliveryHeuristics.ForBehavior(new[] { B("ChargedAttack", 60) }));
        Assert.Equal("nova", DeliveryHeuristics.ForBehavior(new[] { B("ChargedAttack", 40), B("Jump", 30) }));
    }

    [Fact]
    public void RapidStanding_IsProjectile_RapidMobile_IsArc()
    {
        Assert.Equal("projectile", DeliveryHeuristics.ForBehavior(new[] { B("RangedAttack", 60) }));
        Assert.Equal("arc", DeliveryHeuristics.ForBehavior(new[] { B("RangedAttack", 40), B("Dodge", 30), B("Jump", 15) }));
    }

    [Fact]
    public void Melee_IsBurst_RegardlessOfMobility()
    {
        Assert.Equal("burst", DeliveryHeuristics.ForBehavior(new[] { B("MeleeAttack", 50) }));
        Assert.Equal("burst", DeliveryHeuristics.ForBehavior(new[] { B("MeleeAttack", 50), B("Jump", 40) }));
    }

    [Fact]
    public void NoAttack_DefaultsToBeam()
        => Assert.Equal("beam", DeliveryHeuristics.ForBehavior(new[] { B("Jump", 30) }));

    [Fact]
    public void FivePlayStyles_YieldFiveDistinctDeliveries()
    {
        var styles = new[]
        {
            DeliveryHeuristics.ForBehavior(new[] { B("ChargedAttack", 60) }),                 // beam
            DeliveryHeuristics.ForBehavior(new[] { B("ChargedAttack", 40), B("Jump", 40) }),  // nova
            DeliveryHeuristics.ForBehavior(new[] { B("RangedAttack", 60) }),                  // projectile
            DeliveryHeuristics.ForBehavior(new[] { B("RangedAttack", 40), B("Dodge", 40) }),  // arc
            DeliveryHeuristics.ForBehavior(new[] { B("MeleeAttack", 60) }),                   // burst
        };
        Assert.Equal(5, styles.Distinct().Count());
    }
}
