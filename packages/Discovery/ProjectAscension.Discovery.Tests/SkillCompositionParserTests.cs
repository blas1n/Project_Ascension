using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class SkillCompositionParserTests
{
    [Fact]
    public void ParsesCleanJson()
    {
        const string json =
            """{"name":"Arcane Bolt","description":"A focused bolt.","primitives":[{"kind":"Projectile","magnitude":2},{"kind":"Homing","magnitude":1}]}""";

        var skill = SkillCompositionParser.TryParse(json);

        Assert.NotNull(skill);
        Assert.Equal("Arcane Bolt", skill!.Name);
        Assert.Equal(2, skill.Primitives.Count);
        Assert.Equal(PrimitiveKind.Projectile, skill.Primitives[0].Kind);
        Assert.Equal(2, skill.Primitives[0].Magnitude);
    }

    [Fact]
    public void ParsesAndNormalizesTheComposedDeliveryStyle()
    {
        var known = SkillCompositionParser.TryParse(
            """{"name":"X","description":"d","delivery":"Burst","primitives":[{"kind":"Area","magnitude":2}]}""");
        Assert.Equal("burst", known!.Delivery); // normalized to lowercase

        var unknown = SkillCompositionParser.TryParse(
            """{"name":"X","description":"d","delivery":"teleport-swarm","primitives":[{"kind":"Area","magnitude":2}]}""");
        Assert.Equal(string.Empty, unknown!.Delivery); // unknown → "" (executor derives)

        var missing = SkillCompositionParser.TryParse(
            """{"name":"X","description":"d","primitives":[{"kind":"Area","magnitude":2}]}""");
        Assert.Equal(string.Empty, missing!.Delivery);
    }

    [Fact]
    public void ExtractsObjectFromProseAndFences()
    {
        const string text =
            "Sure! Here's the skill:\n```json\n{\"name\":\"Ward\",\"description\":\"d\",\"primitives\":[{\"kind\":\"Shield\",\"magnitude\":1}]}\n```\nHope that helps.";

        var skill = SkillCompositionParser.TryParse(text);

        Assert.NotNull(skill);
        Assert.Equal("Ward", skill!.Name);
        Assert.Equal(PrimitiveKind.Shield, skill.Primitives[0].Kind);
    }

    [Fact]
    public void CaseInsensitiveKind()
    {
        const string json = """{"name":"x","description":"d","primitives":[{"kind":"dAsH","magnitude":1}]}""";
        var skill = SkillCompositionParser.TryParse(json);
        Assert.Equal(PrimitiveKind.Dash, skill!.Primitives[0].Kind);
    }

    [Fact]
    public void ParsesRangeAndDuration()
    {
        const string json =
            """{"name":"x","description":"d","primitives":[{"kind":"Area","magnitude":1,"range":2,"duration":3}]}""";
        var skill = SkillCompositionParser.TryParse(json);
        Assert.Equal(2, skill!.Primitives[0].Range);
        Assert.Equal(3, skill.Primitives[0].Duration);
    }

    [Fact]
    public void MissingParameters_DefaultToZero()
    {
        const string json = """{"name":"x","description":"d","primitives":[{"kind":"Dash","magnitude":1}]}""";
        var skill = SkillCompositionParser.TryParse(json);
        Assert.Equal(0, skill!.Primitives[0].Range);
        Assert.Equal(0, skill.Primitives[0].Duration);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no json here")]
    [InlineData("{not valid json")]
    [InlineData("""{"name":"x","description":"d","primitives":[{"kind":"Nonexistent","magnitude":1}]}""")]
    [InlineData("""{"name":"x","description":"d"}""")] // missing primitives
    public void ReturnsNullOnUnusable(string text)
    {
        Assert.Null(SkillCompositionParser.TryParse(text));
    }
}

public class SkillCompositionPromptTests
{
    [Fact]
    public void IncludesBudgetBehaviorAndEveryPrimitive()
    {
        var request = new CompositionRequest(
            "arcane fire", new[] { "arcane", "firearm" }, PrimitiveKind.Projectile, new PowerBudget(30));

        var prompt = SkillCompositionPrompt.Build(request);

        Assert.Contains("30", prompt);
        Assert.Contains("Projectile", prompt);
        Assert.Contains("arcane", prompt);
        Assert.Contains("Offensive", prompt); // primitives are grouped by category
        Assert.Contains("Defensive", prompt);
        foreach (var def in PrimitiveCatalog.All)
            Assert.Contains(def.Kind.ToString(), prompt);
    }

    [Fact]
    public void IncludesLineageWhenPresent()
    {
        var lineage = new[]
        {
            new PriorArt("Flame Bullet", "A small fiery dart.",
                new[] { new ComposedPrimitive(PrimitiveKind.Projectile, 2) }),
        };
        var request = new CompositionRequest(
            "compressed fire", new[] { "fire" }, PrimitiveKind.Projectile, new PowerBudget(30), lineage);

        var prompt = SkillCompositionPrompt.Build(request);

        Assert.Contains("Flame Bullet", prompt);
        Assert.Contains("builds on", prompt);
    }

    [Fact]
    public void OmitsLineageSectionWhenEmpty()
    {
        var request = new CompositionRequest(
            "fire", new[] { "fire" }, PrimitiveKind.Projectile, new PowerBudget(30));

        Assert.DoesNotContain("builds on the player's prior discoveries", SkillCompositionPrompt.Build(request));
    }
}
