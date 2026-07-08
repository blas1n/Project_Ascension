using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class PrimitiveGraphTranslatorTests
{
    [Fact]
    public void Offensive_BecomesOnCast_AndValidatesInBudget()
    {
        var graph = PrimitiveGraphTranslator.Translate(new[]
        {
            new ComposedPrimitive(PrimitiveKind.Projectile, 3),
            new ComposedPrimitive(PrimitiveKind.Chain, 2),
            new ComposedPrimitive(PrimitiveKind.DamageOverTime, 1, Duration: 2),
        });
        Assert.Equal(TriggerKind.OnCast, Assert.IsType<Trigger>(graph).Kind);
        Assert.True(EffectGraphValidator.Validate(graph, new PowerBudget(100)).IsValid);
    }

    [Fact]
    public void Mobility_BecomesOnJumpInAir()
    {
        var graph = PrimitiveGraphTranslator.Translate(new[]
        {
            new ComposedPrimitive(PrimitiveKind.Dash, 2), new ComposedPrimitive(PrimitiveKind.Blink, 1),
        });
        Assert.Equal(TriggerKind.OnJumpInAir, Assert.IsType<Trigger>(graph).Kind);
    }

    [Fact]
    public void Defensive_BecomesContinuousWard()
    {
        var graph = PrimitiveGraphTranslator.Translate(new[]
        {
            new ComposedPrimitive(PrimitiveKind.Shield, 3), new ComposedPrimitive(PrimitiveKind.Leech, 1),
        });
        Assert.Equal(TriggerKind.Continuous, Assert.IsType<Trigger>(graph).Kind);
    }

    [Fact]
    public void Translated_RoundTripsThroughJson()
    {
        var graph = PrimitiveGraphTranslator.Translate(new[]
        {
            new ComposedPrimitive(PrimitiveKind.Beam, 2), new ComposedPrimitive(PrimitiveKind.Stun, 1),
        });
        var json = EffectGraphJson.Serialize(graph);
        Assert.Equal(json, EffectGraphJson.Serialize(EffectGraphJson.Parse(json)!));
    }
}
