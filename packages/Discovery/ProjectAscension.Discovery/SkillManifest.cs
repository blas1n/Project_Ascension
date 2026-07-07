namespace ProjectAscension.SkillForge;

/// <summary>How a discovered skill manifests for the player to use.</summary>
public enum ManifestationKind
{
    /// <summary>A castable, offensive composition — synthesized magic that becomes a
    /// new equippable weapon the player aims and fires.</summary>
    Weapon,

    /// <summary>A mobility / control technique — a command the player actively invokes
    /// by a button combo (double jump, dodge-slash, a stun burst).</summary>
    Command,

    /// <summary>A defensive composition — an always-on passive (persistent ward /
    /// damage reduction / lifesteal), not invoked.</summary>
    Passive,
}

/// <summary>
/// Classifies a composed skill into how the player wields it (design note: "magic
/// synthesized from magic becomes a new weapon; everything else is a command"). The
/// decision is deterministic and server-authoritative — driven by which primitive
/// category dominates the composition's power, not by the AI.
/// </summary>
public static class SkillManifest
{
    // (The flat-primitive Classify was retired with primitive generation — ADR 0007 Phase 4c;
    // manifestation is derived from the effect graph now, see ManifestationFromGraph. IsMagicContext
    // stays: the graph classifier still needs to know whether the discovery was magic.)

    /// <summary>Does a discovery's context indicate MAGIC — an arcane catalyst or a
    /// synthesized spell-weapon (its "spell:" tag)? Only then can an offensive synthesis
    /// become a new weapon.</summary>
    public static bool IsMagicContext(IEnumerable<string> contextTags)
    {
        if (contextTags is null) return false;
        foreach (var t in contextTags)
            if (string.Equals(t, "arcane", System.StringComparison.OrdinalIgnoreCase)
                || (t != null && t.StartsWith("spell:", System.StringComparison.OrdinalIgnoreCase)))
                return true;
        return false;
    }
}
