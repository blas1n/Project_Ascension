using System.Linq;
using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class ComboAssignerTests
{
    [Fact]
    public void SingleBehavior_RepeatsIt()
    {
        // Double jump (discovered from jumping) → jump, jump — the conventional feel.
        Assert.Equal(
            new[] { InputToken.Jump, InputToken.Jump },
            ComboAssigner.Assign(new[] { "Jump" }, "seed"));
    }

    [Fact]
    public void MultiBehavior_MapsToButtons()
    {
        // Dodge-then-attack → dodge, left-click.
        Assert.Equal(
            new[] { InputToken.Dodge, InputToken.LeftClick },
            ComboAssigner.Assign(new[] { "Dodge", "MeleeAttack" }, "seed"));
    }

    [Fact]
    public void DropsDerivedAndUnknownBehaviors()
    {
        // DodgeAttack is a derived signal; map only the raw inputs.
        Assert.Equal(
            new[] { InputToken.Dodge, InputToken.LeftClick },
            ComboAssigner.Assign(new[] { "Dodge", "MeleeAttack", "DodgeAttack" }, "seed"));
    }

    [Fact]
    public void NoBehaviors_FallsBackToDeterministicSeed()
    {
        var a = ComboAssigner.Assign(null, "discovery-1");
        var b = ComboAssigner.Assign(System.Array.Empty<string>(), "discovery-1");

        Assert.Equal(a, b);                                                  // deterministic
        Assert.InRange(a.Count, ComboAssigner.MinLength, ComboAssigner.MaxLength);
    }

    [Fact]
    public void Fallback_ProducesVariety()
    {
        var distinct = Enumerable.Range(0, 50)
            .Select(i => string.Join(",", ComboAssigner.Assign(null, $"seed-{i}")))
            .Distinct()
            .Count();
        Assert.True(distinct > 5);
    }

    [Fact]
    public void NoTrivialImmediateRepeats_InMappedOrFallback()
    {
        // Mapped combos never put two of the same in a row except the deliberate
        // single-behavior repeat; the fallback never does.
        for (int i = 0; i < 50; i++)
        {
            var combo = ComboAssigner.Assign(null, $"seed-{i}");
            for (int j = 1; j < combo.Count; j++)
                Assert.NotEqual(combo[j - 1], combo[j]);
        }
    }
}
