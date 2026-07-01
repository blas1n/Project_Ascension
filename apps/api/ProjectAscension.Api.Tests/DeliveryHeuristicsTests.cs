using System.Linq;
using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;
using Xunit;

namespace ProjectAscension.Api.Tests;

// The delivery is derived from play (the LLM's own pick converges — see the variety
// simulation). These lock the deterministic mapping so the manifestation stays varied.
public class DeliveryHeuristicsTests
{
    private static BehaviorCount B(string behavior, int count) => new(behavior, count);

    [Theory]
    [InlineData("ChargedAttack", "beam")]
    [InlineData("RangedAttack", "projectile")]
    [InlineData("MeleeAttack", "burst")]
    public void ForBehavior_MapsDominantAttackToADelivery(string attack, string expected)
        => Assert.Equal(expected, DeliveryHeuristics.ForBehavior(new[] { B(attack, 50) }));

    [Fact]
    public void ForBehavior_UsesTheDominantAttack_MovementIgnored()
    {
        // Charging while jumping is still a charge → beam; the footwork doesn't decide it.
        Assert.Equal("beam", DeliveryHeuristics.ForBehavior(new[] { B("ChargedAttack", 40), B("Jump", 60) }));
        // Rapid fire with dodging is still ranged → projectile.
        Assert.Equal("projectile", DeliveryHeuristics.ForBehavior(new[] { B("RangedAttack", 40), B("Dodge", 30) }));
    }

    [Fact]
    public void ForBehavior_NoAttack_DefaultsToBeam()
    {
        Assert.Equal("beam", DeliveryHeuristics.ForBehavior(new[] { B("Jump", 30), B("Dodge", 20) }));
        Assert.Equal("beam", DeliveryHeuristics.ForBehavior(System.Array.Empty<BehaviorCount>()));
    }

    [Fact]
    public void DifferentAttackStyles_YieldDifferentDeliveries()
    {
        var charge = DeliveryHeuristics.ForBehavior(new[] { B("ChargedAttack", 50) });
        var rapid = DeliveryHeuristics.ForBehavior(new[] { B("RangedAttack", 50) });
        var melee = DeliveryHeuristics.ForBehavior(new[] { B("MeleeAttack", 50) });
        Assert.Equal(3, new[] { charge, rapid, melee }.Distinct().Count());
    }
}
