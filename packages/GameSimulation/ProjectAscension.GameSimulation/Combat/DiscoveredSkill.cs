#nullable enable
using System.Collections.Generic;
using ProjectAscension.GameSimulation.Effects;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>How a discovered skill is wielded. Mirrors the server's classification
    /// (ProjectAscension.SkillForge.ManifestationKind); names match so the API's
    /// manifestation string parses straight in.</summary>
    public enum ManifestationKind
    {
        Weapon,
        Command,
        Passive,
    }

    /// <summary>A discovered, executable skill plus how the player wields it — a
    /// synthesized-magic <see cref="ManifestationKind.Weapon"/> (a new equippable
    /// weapon, equipped and fired) or an invoked <see cref="ManifestationKind.Command"/>
    /// (a button-combo technique). No equipment use-gate (ADR 0005). <see cref="Combo"/> is
    /// the command's assigned invocation combo (empty for weapons/passives) — the guide HUD
    /// shows it so the player knows how to trigger the command.</summary>
    public sealed record DiscoveredSkill(
        string Name, ManifestationKind Manifestation, Skill Skill,
        IReadOnlyList<InputToken>? Combo = null,
        // The discovery's context tags (equipment + situation at discovery). A command whose
        // combo uses a weapon click is gated by its equipment tags — ADR 0005 (재개정).
        IReadOnlyList<string>? ContextTags = null,
        // The AI-composed flavor description (a sentence, like a real game's skill text).
        string? Description = null,
        // The AI-composed effect graph (ADR 0007) the runtime interprets. Null only for a legacy
        // graphless DTO before translation; prefer EffectiveGraph, which is never null.
        EffectNode? Graph = null)
    {
        /// <summary>The skill's effect graph, always present (ADR 0007 Phase 4c-4) — the composed
        /// graph, or a deterministic translation of the primitives for a legacy graphless skill.
        /// Consumers use this so the runtime is single-path (no primitive fallback).</summary>
        public EffectNode EffectiveGraph => Graph ?? PrimitiveGraphTranslator.Translate(Skill);
    }
}
