using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class EffectGraphBudgetPackerTests
{
    [Fact]
    public void WithinBudget_IsUnchanged()
    {
        var graph = new Trigger(TriggerKind.OnCast, new Emit(EmitDelivery.Projectile, 1)); // cost 6
        var packed = EffectGraphBudgetPacker.Pack(graph, new PowerBudget(50));
        Assert.Equal(EffectGraphJson.Serialize(graph), EffectGraphJson.Serialize(packed));
    }

    [Fact]
    public void OverBudget_IsClampedToFit_AndValidates()
    {
        // A coherent offensive graph at cost 30 against budget 19 (the tight-budget defer case).
        var graph = new Trigger(TriggerKind.OnCast, new Sequence(new EffectNode[]
        {
            new Emit(EmitDelivery.Burst, 2), new Damage(2), new Dot(1, 2), new Spread(2),
        }));
        Assert.Equal(30, EffectGraph.Cost(graph));

        var packed = EffectGraphBudgetPacker.Pack(graph, new PowerBudget(19));

        Assert.True(EffectGraph.Cost(packed) <= 19);
        Assert.True(EffectGraphValidator.Validate(packed, new PowerBudget(19)).IsValid);
        Assert.Equal(TriggerKind.OnCast, Assert.IsType<Trigger>(packed).Kind); // trigger preserved
    }

    [Fact]
    public void LowersTiersBeforeDroppingNodes()
    {
        // Emit t3 (12) + Damage t3 (12) = 24 vs budget 12 — reachable by lowering tiers alone
        // (Emit t0 3 + Damage t0 3 = 6), so both effects should survive.
        var graph = new Trigger(TriggerKind.OnCast, new Sequence(new EffectNode[]
        {
            new Emit(EmitDelivery.Beam, 3), new Damage(3),
        }));
        var packed = EffectGraphBudgetPacker.Pack(graph, new PowerBudget(12));
        var seq = Assert.IsType<Sequence>(Assert.IsType<Trigger>(packed).Child);
        Assert.Equal(2, seq.Steps.Count);                 // nothing dropped
        Assert.True(EffectGraph.Cost(packed) <= 12);
    }

    [Fact]
    public void ImpossiblyTightBudget_KeepsAtLeastOneEffect()
    {
        var graph = new Trigger(TriggerKind.OnCast, new Sequence(new EffectNode[]
        {
            new Emit(EmitDelivery.Nova, 3), new Damage(3), new Control(ControlEffect.Stun, 3),
        }));
        var packed = EffectGraphBudgetPacker.Pack(graph, new PowerBudget(1)); // below any single node
        var trigger = Assert.IsType<Trigger>(packed);
        Assert.NotNull(trigger.Child); // never empties the effect
    }
}
