using System;
using System.Collections.Generic;
using ProjectAscension.GameSimulation.Discovery;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Derives a command's invocation combo from the behaviors that discovered it: the
    /// distinct raw inputs, in order of first appearance. The derived
    /// <see cref="BehaviorKind.DodgeAttack"/> signal is dropped — it is itself a
    /// dodge→attack combo, recognized from the raw inputs.
    /// </summary>
    public static class ComboBuilder
    {
        public static IReadOnlyList<BehaviorKind> FromBehaviors(IEnumerable<string> behaviorNames)
        {
            var combo = new List<BehaviorKind>();
            if (behaviorNames == null) return combo;
            foreach (var name in behaviorNames)
            {
                if (!Enum.TryParse<BehaviorKind>(name, ignoreCase: true, out var kind)) continue;
                if (kind == BehaviorKind.DodgeAttack) continue; // derived, not a raw input
                if (!combo.Contains(kind)) combo.Add(kind);
            }
            return combo;
        }
    }
}
