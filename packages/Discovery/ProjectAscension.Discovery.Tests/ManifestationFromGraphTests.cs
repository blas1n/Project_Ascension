using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class ManifestationFromGraphTests
{
    [Fact]
    public void NoGraph_ReturnsNull_SoCallerFallsBackToPrimitives()
        => Assert.Null(ManifestationFromGraph.Classify(null, magicContext: true));

    [Fact]
    public void OnCastOffensive_InMagicContext_IsWeapon()
    {
        var graph = new Trigger(TriggerKind.OnCast,
            new Sequence(new EffectNode[] { new Emit(EmitDelivery.Projectile, 1), new Damage(2) }));
        Assert.Equal(ManifestationKind.Weapon, ManifestationFromGraph.Classify(graph, magicContext: true));
    }

    [Fact]
    public void OnCastOffensive_WithoutMagic_IsCommand()
    {
        var graph = new Trigger(TriggerKind.OnCast,
            new Sequence(new EffectNode[] { new Emit(EmitDelivery.Projectile, 1), new Damage(2) }));
        Assert.Equal(ManifestationKind.Command, ManifestationFromGraph.Classify(graph, magicContext: false));
    }

    [Fact]
    public void OnCastControlDominant_IsCommand_EvenInMagicContext()
    {
        var graph = new Trigger(TriggerKind.OnCast, new Control(ControlEffect.Stun, 2));
        Assert.Equal(ManifestationKind.Command, ManifestationFromGraph.Classify(graph, magicContext: true));
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
    public void OnDodge_PureMovement_IsPassive_ButDodgeAttack_IsCommand()
    {
        Assert.Equal(ManifestationKind.Passive,
            ManifestationFromGraph.Classify(new Trigger(TriggerKind.OnDodge, new Impulse(ImpulseDirection.Forward, 1)), false));
        Assert.Equal(ManifestationKind.Command,
            ManifestationFromGraph.Classify(new Trigger(TriggerKind.OnDodge,
                new Sequence(new EffectNode[] { new Impulse(ImpulseDirection.Forward, 1), new Damage(1) })), true));
    }

    [Fact]
    public void ContinuousWard_IsPassive()
        => Assert.Equal(ManifestationKind.Passive,
            ManifestationFromGraph.Classify(new Trigger(TriggerKind.Continuous, new Ward(WardEffect.Shield, 1)), true));
}
