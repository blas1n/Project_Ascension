using System.Linq;
using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class ComboAssignerTests
{
    [Fact]
    public void IsDeterministic()
    {
        Assert.Equal(ComboAssigner.Assign("discovery-123"), ComboAssigner.Assign("discovery-123"));
    }

    [Fact]
    public void ProducesVariety()
    {
        // Across many discoveries the assigned combos should spread, not collapse to one.
        var distinct = Enumerable.Range(0, 50)
            .Select(i => string.Join(",", ComboAssigner.Assign($"seed-{i}")))
            .Distinct()
            .Count();
        Assert.True(distinct > 5);
    }

    [Fact]
    public void RespectsLengthBounds()
    {
        var combo = ComboAssigner.Assign("anything");
        Assert.InRange(combo.Count, ComboAssigner.MinLength, ComboAssigner.MaxLength);
    }

    [Fact]
    public void NoTrivialImmediateRepeats()
    {
        // Single-behavior discoveries still get a real combo (the point of unifying
        // commands), without degenerate runs like Jump, Jump.
        for (int i = 0; i < 50; i++)
        {
            var combo = ComboAssigner.Assign($"seed-{i}");
            for (int j = 1; j < combo.Count; j++)
                Assert.NotEqual(combo[j - 1], combo[j]);
        }
    }
}
