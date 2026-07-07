#nullable enable
using System.Collections.Generic;
using ProjectAscension.GameSimulation.Effects;

namespace ProjectAscension.GameSimulation.Player
{
    /// <summary>
    /// The movement the player has gained from discovered skills, read off their effect GRAPHS
    /// (ADR 0007) rather than a bespoke field. A movement trigger over an upward impulse is an
    /// extra air jump (double jump); a wall-contact trigger is a wall-climb. Extends without an
    /// engine change — a new movement trigger just adds a case here.
    /// </summary>
    public sealed record MovementCapability(int ExtraJumps, bool WallClimb)
    {
        public const int MaxExtraJumps = 2;

        public static readonly MovementCapability None = new(0, false);

        /// <summary>Fold every discovered skill's graph into the total movement capability. Only
        /// movement triggers contribute; offensive/defensive graphs are ignored here.</summary>
        public static MovementCapability From(IEnumerable<EffectNode?> graphs)
        {
            int extraJumps = 0;
            bool wallClimb = false;
            if (graphs != null)
            {
                foreach (var graph in graphs)
                {
                    if (!(graph is Trigger t)) continue;
                    switch (t.Kind)
                    {
                        // On an in-air jump (or a dodge used as a movement tech), an upward impulse
                        // = one extra air jump. Double jump with no special-case.
                        case TriggerKind.OnJumpInAir:
                        case TriggerKind.OnDodge:
                            if (HasUpwardImpulse(t.Child)) extraJumps++;
                            break;
                        case TriggerKind.OnWallContact:
                            wallClimb = true;
                            break;
                    }
                }
            }
            if (extraJumps > MaxExtraJumps) extraJumps = MaxExtraJumps;
            return new MovementCapability(extraJumps, wallClimb);
        }

        private static bool HasUpwardImpulse(EffectNode node)
        {
            switch (node)
            {
                case Impulse imp:
                    return imp.Direction == ImpulseDirection.Up || imp.Direction == ImpulseDirection.Forward;
                case Sequence seq:
                    foreach (var step in seq.Steps)
                        if (HasUpwardImpulse(step)) return true;
                    return false;
                default:
                    return false;
            }
        }
    }
}
