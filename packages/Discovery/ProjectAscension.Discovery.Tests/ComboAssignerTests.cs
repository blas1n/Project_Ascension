using System.Collections.Generic;
using System.Linq;
using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class ComboAssignerTests
{
    private static bool StartsWith(IReadOnlyList<InputToken> seq, IReadOnlyList<InputToken> prefix)
    {
        if (prefix.Count > seq.Count) return false;
        for (int i = 0; i < prefix.Count; i++)
            if (seq[i] != prefix[i]) return false;
        return true;
    }

    private static bool Collides(IReadOnlyList<InputToken> a, IReadOnlyList<InputToken> b)
        => StartsWith(a, b) || StartsWith(b, a);

    [Fact]
    public void EnsurePrefixFree_KeepsCandidate_WhenNoCollision()
    {
        var candidate = new[] { InputToken.Jump, InputToken.Dodge };
        var result = ComboAssigner.EnsurePrefixFree(candidate, System.Array.Empty<IReadOnlyList<InputToken>>(), "s");
        Assert.Equal(candidate, result);
    }

    [Fact]
    public void EnsurePrefixFree_ResolvesCollision_WhenCandidateIsPrefixOfExisting()
    {
        var existing = new IReadOnlyList<InputToken>[]
            { new[] { InputToken.Dodge, InputToken.Jump, InputToken.RightClick } };
        var result = ComboAssigner.EnsurePrefixFree(new[] { InputToken.Dodge, InputToken.Jump }, existing, "s1");
        Assert.False(Collides(result, existing[0]));
    }

    [Fact]
    public void EnsurePrefixFree_ResolvesCollision_WhenExistingIsPrefixOfCandidate()
    {
        var existing = new IReadOnlyList<InputToken>[] { new[] { InputToken.Dodge, InputToken.Jump } };
        var result = ComboAssigner.EnsurePrefixFree(
            new[] { InputToken.Dodge, InputToken.Jump, InputToken.RightClick }, existing, "s2");
        Assert.False(Collides(result, existing[0]));
    }

    [Fact]
    public void EnsurePrefixFree_KeepsTheWholeSetPrefixFree()
    {
        var taken = new List<IReadOnlyList<InputToken>>();
        for (int i = 0; i < 20; i++)
        {
            var candidate = ComboAssigner.Assign(null, $"seed-{i}");
            var combo = ComboAssigner.EnsurePrefixFree(candidate, taken, $"seed-{i}");
            foreach (var e in taken)
                Assert.False(Collides(combo, e));
            taken.Add(combo);
        }
    }

    [Fact]
    public void Parse_RoundTripsTokenNames()
    {
        Assert.Equal(
            new[] { InputToken.Jump, InputToken.RightClick },
            ComboAssigner.Parse(new[] { "Jump", "RightClick" }).ToArray());
    }

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
