using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

// ADR 0007 Phase 1 — the effect-graph model + validator. The AI will later compose these graphs;
// these tests pin the deterministic cost/validation the engine owns.
public class EffectGraphTests
{
    [Fact]
    public void DoubleJump_IsATriggerWithAnUpwardImpulse_AndValidates()
    {
        // No bespoke ExtraJumps: double jump = "on jump-in-air, an upward impulse".
        var graph = new Trigger(TriggerKind.OnJumpInAir, new Impulse(ImpulseDirection.Up, 1));
        var result = EffectGraphValidator.Validate(graph, new PowerBudget(50));
        Assert.True(result.IsValid);
        Assert.Equal(2, EffectGraph.NodeCount(graph)); // trigger + impulse
    }

    [Fact]
    public void Fireball_SequenceUnderBudget_Validates()
    {
        var graph = new Trigger(TriggerKind.OnCast,
            new Sequence(new EffectNode[] { new Emit(EmitDelivery.Projectile, 1), new Damage(2) }));
        Assert.True(EffectGraphValidator.Validate(graph, new PowerBudget(50)).IsValid);
    }

    [Fact]
    public void OverBudget_Fails()
    {
        // Cost is STRUCTURAL now (ADR 0010): a Control costs 5 whatever its magnitude. Three effects
        // cost 5+4+3 = 12, and a 6-point budget cannot hold them.
        var graph = new Trigger(TriggerKind.OnCast, new Sequence(new EffectNode[]
        {
            new Control(ControlEffect.Stun, 0), new Ward(WardEffect.Shield, 0), new Emit(EmitDelivery.Beam, 0),
        }));
        var result = EffectGraphValidator.Validate(graph, new PowerBudget(6));
        Assert.False(result.IsValid);
        Assert.Equal(CompositionError.OverBudget, result.Error);
    }

    [Fact]
    public void RootMustBeATrigger()
        => Assert.False(EffectGraphValidator.Validate(new Damage(1), new PowerBudget(50)).IsValid);

    [Fact]
    public void NestedTriggers_AreRejected()
    {
        var nested = new Trigger(TriggerKind.OnCast, new Trigger(TriggerKind.OnHit, new Damage(1)));
        Assert.False(EffectGraphValidator.Validate(nested, new PowerBudget(50)).IsValid);
    }

    [Fact]
    public void EmptySequence_IsRejected()
    {
        var empty = new Trigger(TriggerKind.OnCast, new Sequence(new EffectNode[0]));
        Assert.False(EffectGraphValidator.Validate(empty, new PowerBudget(50)).IsValid);
    }

    [Fact]
    public void TierOutOfRange_Fails()
    {
        var graph = new Trigger(TriggerKind.OnCast, new Damage(99));
        Assert.Equal(CompositionError.InvalidMagnitude,
            EffectGraphValidator.Validate(graph, new PowerBudget(50)).Error);
    }

    [Fact]
    public void Cost_IsDeterministicAndAdditive()
    {
        var g = new Trigger(TriggerKind.OnCast,
            new Sequence(new EffectNode[] { new Emit(EmitDelivery.Beam, 1), new Damage(1) }));
        Assert.Equal(EffectGraph.Cost(g), EffectGraph.Cost(g)); // pure
        // Structural: Emit 3 + Damage 3, the trigger is free — and the TIER is free too, because
        // magnitude is not for sale (ADR 0010).
        Assert.Equal(3 + 3, EffectGraph.Cost(g));
    }
}
