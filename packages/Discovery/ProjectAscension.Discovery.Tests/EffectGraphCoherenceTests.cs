using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

/// <summary>
/// The validator rejects graphs that pass every structural check but would be DEAD at runtime — a
/// trigger whose effects no interpreter acts on (audit-found: a movement trigger with no impulse,
/// a Continuous with no ward, an OnCast with no real effect). These pin those coherence rules so a
/// validated graph always does something.
/// </summary>
public class EffectGraphCoherenceTests
{
    private static readonly PowerBudget Budget = new(60);
    private static bool Valid(EffectNode g) => EffectGraphValidator.Validate(g, Budget).IsValid;

    // --- coherent shapes still validate ---

    [Fact]
    public void MovementTrigger_WithImpulse_IsCoherent()
    {
        Assert.True(Valid(new Trigger(TriggerKind.OnJumpInAir, new Impulse(ImpulseDirection.Up, 1))));
        Assert.True(Valid(new Trigger(TriggerKind.OnWallContact, new Impulse(ImpulseDirection.Up, 2))));
    }

    [Fact]
    public void Continuous_WithWard_IsCoherent()
        => Assert.True(Valid(new Trigger(TriggerKind.Continuous, new Ward(WardEffect.Shield, 1))));

    [Fact]
    public void OnCast_WithARealEffect_IsCoherent()
    {
        Assert.True(Valid(new Trigger(TriggerKind.OnCast, new Emit(EmitDelivery.Projectile, 1))));
        Assert.True(Valid(new Trigger(TriggerKind.OnCast, new Dot(2, 3))));           // DoT is a real hit
        Assert.True(Valid(new Trigger(TriggerKind.OnCast, new Control(ControlEffect.Stun, 1))));
    }

    [Fact]
    public void OnDodge_AsMovementOrAttack_IsCoherent()
    {
        Assert.True(Valid(new Trigger(TriggerKind.OnDodge, new Impulse(ImpulseDirection.Forward, 1)))); // movement
        Assert.True(Valid(new Trigger(TriggerKind.OnDodge,
            new Sequence(new EffectNode[] { new Impulse(ImpulseDirection.Forward, 1), new Damage(1) })))); // dodge-attack
    }

    // --- dead shapes are rejected ---

    [Fact]
    public void MovementTrigger_WithoutImpulse_IsRejected()
    {
        // The exact dead skill from playtest: OnJumpInAir + an offensive emit → no movement, dead.
        Assert.False(Valid(new Trigger(TriggerKind.OnJumpInAir, new Emit(EmitDelivery.Burst, 2))));
        Assert.False(Valid(new Trigger(TriggerKind.OnWallContact, new Damage(2))));
    }

    [Fact]
    public void Continuous_WithoutWard_IsRejected()
    {
        Assert.False(Valid(new Trigger(TriggerKind.Continuous, new Emit(EmitDelivery.Nova, 2))));
        Assert.False(Valid(new Trigger(TriggerKind.Continuous, new Damage(2))));
        Assert.False(Valid(new Trigger(TriggerKind.Continuous, new Ward(WardEffect.Heal, 2)))); // Heal isn't always-on
    }

    [Fact]
    public void OnCast_WithoutARealEffect_IsRejected()
    {
        Assert.False(Valid(new Trigger(TriggerKind.OnCast, new Impulse(ImpulseDirection.Up, 2))));  // mobility only
        Assert.False(Valid(new Trigger(TriggerKind.OnCast, new Homing(2))));                        // targeting only
        Assert.False(Valid(new Trigger(TriggerKind.OnCast,
            new Sequence(new EffectNode[] { new Homing(1), new Spread(1) }))));                     // riders, no hit
    }

    [Fact]
    public void OnHit_IsRejected_UntilItHasAnInterpreter()
        => Assert.False(Valid(new Trigger(TriggerKind.OnHit, new Emit(EmitDelivery.Projectile, 1))));
}
