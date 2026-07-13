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
        // Cost is structural now (ADR 0010): Emit 3 + Damage 3 + Dot 4 + Spread 3 = 13, whatever the
        // tiers. An over-budget graph gives up an EFFECT — it never gives up its teeth.
        var graph = new Trigger(TriggerKind.OnCast, new Sequence(new EffectNode[]
        {
            new Emit(EmitDelivery.Burst, 2), new Damage(2), new Dot(1, 2), new Spread(2),
        }));
        Assert.Equal(13, EffectGraph.Cost(graph));

        var packed = EffectGraphBudgetPacker.Pack(graph, new PowerBudget(9));

        Assert.True(EffectGraph.Cost(packed) <= 9);
        Assert.True(EffectGraphValidator.Validate(packed, new PowerBudget(9)).IsValid);
        Assert.Equal(TriggerKind.OnCast, Assert.IsType<Trigger>(packed).Kind); // trigger preserved

        // The Dot (the priciest, 4) was surrendered — and the Emit that survived is still tier 2.
        var steps = ((Sequence)((Trigger)packed).Child).Steps;
        Assert.DoesNotContain(steps, n => n is Dot);
        Assert.Contains(steps, n => n is Emit { Tier: 2 });
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
