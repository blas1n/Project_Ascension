using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class SkillManifestTests
{
    // The flat-primitive Classify was retired with primitive generation (ADR 0007 Phase 4c) —
    // manifestation is derived from the effect graph now (see ManifestationFromGraphTests). Only
    // the magic-context detection remains here (the graph classifier still needs it).

    [Fact]
    public void IsMagicContext_DetectsArcaneAndSpellTags()
    {
        Assert.True(SkillManifest.IsMagicContext(new[] { "firearm", "arcane" }));
        Assert.True(SkillManifest.IsMagicContext(new[] { "spell:flame-bolt" }));
        Assert.False(SkillManifest.IsMagicContext(new[] { "bow", "nonmagic" }));
        Assert.False(SkillManifest.IsMagicContext(System.Array.Empty<string>()));
    }
}
