#nullable enable
using System;
using System.Collections.Generic;
using ProjectAscension.GameSimulation.Effects;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Turns a composed-skill server response into a usable <see cref="DiscoveredSkill"/> — the
    /// ACCEPTANCE decision and the core build, extracted from the Unity loaders (GameSession /
    /// SkillCaster) so it can be tested headless. The client restore regression (a graph-only skill
    /// carries no primitives, but the loaders required them — ADR 0007 Phase 4c) lived in that
    /// MonoBehaviour glue, invisible to the headless harness; with the decision here, a contract
    /// test can pin that a graph-only Ready response yields a usable skill. Weapon minting stays in
    /// the Unity factory (it needs WeaponData).
    /// </summary>
    public static class SkillRestore
    {
        /// <summary>Build the skill from a response's fields, or null when it isn't Ready. A skill
        /// is accepted on Ready alone — NOT on having primitives (graph-only skills have none).</summary>
        public static DiscoveredSkill? FromResponse(
            string? status,
            string? name,
            string? manifestation,
            IReadOnlyList<string>? primitives,
            IReadOnlyList<string>? contextTags,
            string? description,
            string? effectGraph,
            // The behaviours that MADE it — the evidence that binds it to a weapon, or leaves it free
            // (ADR 0011). Optional so legacy callers keep compiling.
            IReadOnlyList<string>? behaviors = null)
        {
            if (!string.Equals(status, "Ready", StringComparison.OrdinalIgnoreCase)) return null;

            var skill = SkillParser.Parse(string.IsNullOrEmpty(name) ? "Discovery" : name!, primitives!);
            var kind = Enum.TryParse<ManifestationKind>(manifestation, ignoreCase: true, out var k)
                ? k
                : ManifestationKind.Command;

            // The runtime interprets the effect graph; a legacy graphless (or unparseable) response
            // is translated from its primitives so every skill runs on the graph path (Phase 4c-4).
            EffectNode graph = string.IsNullOrEmpty(effectGraph)
                ? PrimitiveGraphTranslator.Translate(skill)
                : EffectGraphReader.Parse(effectGraph!) ?? PrimitiveGraphTranslator.Translate(skill);

            return new DiscoveredSkill(skill.Name, kind, skill, contextTags, behaviors, description, graph);
        }
    }
}
