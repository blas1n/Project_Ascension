using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class ManifestationFromGraphTests
{
    [Fact]
    public void NoGraph_ReturnsNull_SoCallerFallsBackToPrimitives()
        => Assert.Null(ManifestationFromGraph.Classify(null, magicFusion: true));

    [Fact]
    public void OnCastOffensive_InMagicContext_IsWeapon()
    {
        var graph = new Trigger(TriggerKind.OnCast,
            new Sequence(new EffectNode[] { new Emit(EmitDelivery.Projectile, 1), new Damage(2) }));
        Assert.Equal(ManifestationKind.Weapon, ManifestationFromGraph.Classify(graph, magicFusion: true));
    }

    [Fact]
    public void OnCastOffensive_WithoutMagic_IsCommand()
    {
        var graph = new Trigger(TriggerKind.OnCast,
            new Sequence(new EffectNode[] { new Emit(EmitDelivery.Projectile, 1), new Damage(2) }));
        Assert.Equal(ManifestationKind.Command, ManifestationFromGraph.Classify(graph, magicFusion: false));
    }

    [Fact]
    public void OnCastControlDominant_IsCommand_EvenInMagicContext()
    {
        var graph = new Trigger(TriggerKind.OnCast, new Control(ControlEffect.Stun, 2));
        Assert.Equal(ManifestationKind.Command, ManifestationFromGraph.Classify(graph, magicFusion: true));
    }

    [Fact]
    public void MovementTriggers_ArePassive()
    {
        Assert.Equal(ManifestationKind.Passive,
            ManifestationFromGraph.Classify(new Trigger(TriggerKind.OnJumpInAir, new Impulse(ImpulseDirection.Up, 1)), false));
        Assert.Equal(ManifestationKind.Passive,
            ManifestationFromGraph.Classify(new Trigger(TriggerKind.OnWallContact, new Impulse(ImpulseDirection.Up, 2)), false));
    }

    [Fact]
    public void AWeaponIsBornOfASYNTHESIS_NotOfCarryingACatalyst()
    {
        // Making a weapon is how the game expresses MAGIC SYNTHESIS ("화기 + 술식 → 마력 탄환"), so it takes
        // an actual fusion — two hands woven into one act, one of them magic (ADR 0011).
        Assert.True(ManifestationFromGraph.IsMagicFusion(new[] { "Fuse:arcane>firearm" }));
        Assert.True(ManifestationFromGraph.IsMagicFusion(new[] { "Fuse:melee>arcane" }));
    }

    [Fact]
    public void ASingleSpell_HoweverFierce_IsATechniqueYouInvoke()
    {
        // Not a weapon. A command. Casting the same spell a thousand times forges nothing.
        Assert.False(ManifestationFromGraph.IsMagicFusion(new[] { "Use:arcane", "Chain:arcane", "RangedAttack" }));
    }

    [Fact]
    public void AFusionWithNoMagicInIt_ForgesNothing()
    {
        // Rolling into a gunshot is a fine technique. It is not spellcraft, and it makes no weapon.
        Assert.False(ManifestationFromGraph.IsMagicFusion(new[] { "Fuse:melee>firearm", "Seq:jump>firearm" }));
    }
}
