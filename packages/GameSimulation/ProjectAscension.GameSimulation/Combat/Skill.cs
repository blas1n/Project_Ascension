using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>One effect primitive of an executable skill: its potency
    /// (<see cref="Magnitude"/>) and its reach/persistence tiers
    /// (<see cref="Range"/> / <see cref="Duration"/>). Mirrors the composed primitive
    /// the discovery engine froze.</summary>
    public sealed record SkillPrimitive(SkillPrimitiveKind Kind, int Magnitude, int Range = 0, int Duration = 0);

    /// <summary>
    /// An executable skill — a discovered <c>SkillComposition</c> brought into the
    /// combat simulation. <see cref="SkillResolver"/> turns it into deterministic
    /// combat effects, so an AI-composed discovery actually works in combat.
    /// </summary>
    public sealed record Skill(string Name, IReadOnlyList<SkillPrimitive> Primitives);
}
