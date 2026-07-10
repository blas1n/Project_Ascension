using System;
using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Builds an executable <see cref="Skill"/> from the discovery API's primitive
    /// descriptions — the format <c>"{Kind} x{Magnitude}[ r{Range}][ d{Duration}]"</c>
    /// returned by <c>GET /api/discoveries/{id}/skill</c>. The bridge from a frozen
    /// discovered skill to the combat simulation.
    /// </summary>
    public static class SkillParser
    {
        public static Skill Parse(string name, IEnumerable<string> primitiveDescriptions)
        {
            var primitives = new List<SkillPrimitive>();
            if (primitiveDescriptions != null) // graph-only skills carry none (ADR 0007 Phase 4c)
                foreach (var desc in primitiveDescriptions)
                    if (TryParsePrimitive(desc, out var p))
                        primitives.Add(p);
            return new Skill(name, primitives);
        }

        public static bool TryParsePrimitive(string text, out SkillPrimitive primitive)
        {
            primitive = null!;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 2) return false;
            if (!Enum.TryParse<SkillPrimitiveKind>(tokens[0], ignoreCase: true, out var kind)) return false;

            int magnitude = 0, range = 0, duration = 0;
            for (int i = 1; i < tokens.Length; i++)
            {
                var t = tokens[i];
                if (t.Length < 2 || !int.TryParse(t.Substring(1), out var value)) continue;
                switch (char.ToLowerInvariant(t[0]))
                {
                    case 'x': magnitude = value; break;
                    case 'r': range = value; break;
                    case 'd': duration = value; break;
                }
            }

            if (magnitude <= 0) return false;
            primitive = new SkillPrimitive(kind, magnitude, range, duration);
            return true;
        }
    }
}
