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
    public void MobilityComposition_IsPassive()
    {
        // Mobility (Dash/Blink) is a movement CAPABILITY (double jump), used via the movement
        // input — a passive, not a hotkey command.
        var skill = Of(new ComposedPrimitive(PrimitiveKind.Dash, 3), new ComposedPrimitive(PrimitiveKind.Blink, 1));
        Assert.Equal(ManifestationKind.Passive, SkillManifest.Classify(skill));
    }

    [Fact]
    public void ControlComposition_IsCommand()
    {
        // Control (Stun/Knockback/Slow) is an actively invoked ability → a hotkey command.
        var skill = Of(new ComposedPrimitive(PrimitiveKind.Stun, 3), new ComposedPrimitive(PrimitiveKind.Knockback, 1));
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
    public void MobilityDominantMix_IsPassive()
    {
        // Dash 3 (mobility) outweighs Stun 1 (control) and Projectile 1 (offensive) → a
        // movement-capability passive.
        var skill = Of(
            new ComposedPrimitive(PrimitiveKind.Dash, 3),
            new ComposedPrimitive(PrimitiveKind.Stun, 1),
            new ComposedPrimitive(PrimitiveKind.Projectile, 1));
        Assert.Equal(ManifestationKind.Passive, SkillManifest.Classify(skill));
    }

    [Fact]
    public void DefensiveComposition_IsPassive()
    {
        var skill = Of(new ComposedPrimitive(PrimitiveKind.Leech, 2), new ComposedPrimitive(PrimitiveKind.Barrier, 1));
        Assert.Equal(ManifestationKind.Passive, SkillManifest.Classify(skill));
    }

    [Fact]
    public void DefensiveDominantMix_IsPassive()
    {
        // Shield 3 + Leech 1 (defensive) outweigh Dash 1 (mobility) and Projectile 1.
        var skill = Of(
            new ComposedPrimitive(PrimitiveKind.Shield, 3),
            new ComposedPrimitive(PrimitiveKind.Leech, 1),
            new ComposedPrimitive(PrimitiveKind.Dash, 1),
            new ComposedPrimitive(PrimitiveKind.Projectile, 1));
        Assert.Equal(ManifestationKind.Passive, SkillManifest.Classify(skill));
    }
}
