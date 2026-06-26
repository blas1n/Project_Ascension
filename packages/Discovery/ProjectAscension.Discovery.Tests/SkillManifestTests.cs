using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class SkillManifestTests
{
    private static SkillComposition Of(params ComposedPrimitive[] primitives)
        => new("Test", "desc", primitives);

    [Fact]
    public void OffensiveComposition_IsWeapon()
    {
        var skill = Of(new ComposedPrimitive(PrimitiveKind.Projectile, 3), new ComposedPrimitive(PrimitiveKind.Fork, 1));
        Assert.Equal(ManifestationKind.Weapon, SkillManifest.Classify(skill));
    }

    [Fact]
    public void MobilityComposition_IsCommand()
    {
        var skill = Of(new ComposedPrimitive(PrimitiveKind.Dash, 3), new ComposedPrimitive(PrimitiveKind.Blink, 1));
        Assert.Equal(ManifestationKind.Command, SkillManifest.Classify(skill));
    }

    [Fact]
    public void OffensiveDominantMix_IsWeapon()
    {
        // Projectile 3 (offensive) outweighs Dash 1 (mobility).
        var skill = Of(new ComposedPrimitive(PrimitiveKind.Projectile, 3), new ComposedPrimitive(PrimitiveKind.Dash, 1));
        Assert.Equal(ManifestationKind.Weapon, SkillManifest.Classify(skill));
    }

    [Fact]
    public void UtilityDominantMix_IsCommand()
    {
        // Dash 3 (mobility) + Shield 1 (defensive) outweigh Projectile 1.
        var skill = Of(
            new ComposedPrimitive(PrimitiveKind.Dash, 3),
            new ComposedPrimitive(PrimitiveKind.Shield, 1),
            new ComposedPrimitive(PrimitiveKind.Projectile, 1));
        Assert.Equal(ManifestationKind.Command, SkillManifest.Classify(skill));
    }
}
