using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class EffectGraphJsonTests
{
    // Canonical serialization is the graph's structural signature (records don't deep-compare
    // the Sequence list), so round-trip fidelity is asserted through it.
    private static void RoundTrips(EffectNode graph)
    {
        var json = EffectGraphJson.Serialize(graph);
        var back = EffectGraphJson.Parse(json);
        Assert.NotNull(back);
        Assert.Equal(json, EffectGraphJson.Serialize(back!));
    }

    [Fact]
    public void RoundTrip_DoubleJump()
        => RoundTrips(new Trigger(TriggerKind.OnJumpInAir, new Impulse(ImpulseDirection.Up, 1)));

    [Fact]
    public void RoundTrip_WallClimb()
        // A new mechanic with no engine change: on wall contact, an upward impulse.
        => RoundTrips(new Trigger(TriggerKind.OnWallContact, new Impulse(ImpulseDirection.Up, 2)));

    [Fact]
    public void RoundTrip_FireballSequence()
        => RoundTrips(new Trigger(TriggerKind.OnCast,
            new Sequence(new EffectNode[] { new Emit(EmitDelivery.Projectile, 1), new Damage(2), new Control(ControlEffect.Knockback, 1) })));

    [Fact]
    public void RoundTrip_OffensiveRiders_DotSpreadHoming()
        // Phase 4b vocabulary: a homing projectile that chains, burns, and knocks back.
        => RoundTrips(new Trigger(TriggerKind.OnCast, new Sequence(new EffectNode[]
        {
            new Emit(EmitDelivery.Projectile, 1), new Homing(1), new Spread(2),
            new Damage(1), new Dot(2, 3), new Control(ControlEffect.Knockback, 1),
        })));

    [Fact]
    public void Parse_FromRawJson_BuildsTheGraph()
    {
        const string raw = "{\"trigger\":\"OnCast\",\"effect\":{\"kind\":\"Sequence\",\"steps\":[{\"kind\":\"Emit\",\"delivery\":\"Beam\",\"tier\":1},{\"kind\":\"Damage\",\"tier\":2}]}}";
        var graph = EffectGraphJson.Parse(raw);
        Assert.NotNull(graph);
        Assert.Equal(raw, EffectGraphJson.Serialize(graph!)); // canonical (our serializer's exact shape)
        Assert.IsType<Trigger>(graph);
    }

    [Fact]
    public void ParseComposition_ReadsNameDescriptionAndGraph()
    {
        var comp = EffectGraphJson.ParseComposition(
            "{\"name\":\"Ember Lance\",\"description\":\"A dart of living flame.\"," +
            "\"trigger\":\"OnCast\",\"effect\":{\"kind\":\"Emit\",\"delivery\":\"Projectile\",\"tier\":2}}");
        Assert.NotNull(comp);
        Assert.Equal("Ember Lance", comp!.Name);
        Assert.Equal("A dart of living flame.", comp.Description);
        Assert.IsType<Trigger>(comp.Graph);
    }

    [Fact]
    public void ParseComposition_NullWhenGraphInvalid()
        // name/description alone aren't a skill — a bad graph means no composition.
        => Assert.Null(EffectGraphJson.ParseComposition("{\"name\":\"X\",\"description\":\"y\",\"trigger\":\"Bogus\"}"));

    [Fact]
    public void Parse_Malformed_ReturnsNull()
    {
        Assert.Null(EffectGraphJson.Parse("not json"));
        Assert.Null(EffectGraphJson.Parse("{\"trigger\":\"OnCast\"}"));           // no effect
        Assert.Null(EffectGraphJson.Parse("{\"trigger\":\"Bogus\",\"effect\":{\"kind\":\"Damage\",\"tier\":1}}")); // bad trigger
        Assert.Null(EffectGraphJson.Parse("{\"trigger\":\"OnCast\",\"effect\":{\"kind\":\"Nope\"}}")); // unknown node
    }

    [Fact]
    public void ParsedGraph_ValidatesWithinBudget()
    {
        var graph = EffectGraphJson.Parse("{\"trigger\":\"OnWallContact\",\"effect\":{\"kind\":\"Impulse\",\"direction\":\"Up\",\"tier\":2}}");
        Assert.NotNull(graph);
        Assert.True(EffectGraphValidator.Validate(graph!, new PowerBudget(50)).IsValid);
    }
}
