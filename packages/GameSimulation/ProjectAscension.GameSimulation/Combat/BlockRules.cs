using System.Numerics;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Active blocking with a shield. Deliberately NOT a passive damage reduction: the shield only
    /// protects while the player HOLDS it up, which is the FPS grammar — you decide to defend, and you
    /// give something up (your off hand, your attack) to do it.
    ///
    /// And it only covers the FRONT. A blow from the flank or behind ignores the shield entirely, so
    /// positioning matters and the monster telegraph is a real read: see the wind-up, face it, raise
    /// the shield. Blocking is a decision about damage, so the rule lives here (ADR: Unity is a shell)
    /// — the MonoBehaviour only reports "the shield is up" and where the blow came from.
    /// </summary>
    public static class BlockRules
    {
        /// <summary>Whether a hit actually meets the shield: it must be raised AND the blow must land
        /// inside the shield's front arc. <paramref name="facingDot"/> is dot(playerForward, directionToAttacker)
        /// — 1 is dead ahead, 0 is directly to the side, negative is behind.</summary>
        public static bool Blocks(bool isBlocking, float facingDot, float frontArcDot)
            => isBlocking && facingDot >= frontArcDot;

        /// <summary>The damage that lands through a raised shield. An unblocked blow (shield down, or
        /// struck from outside the front arc) is taken in full.</summary>
        public static float Blocked(float amount, bool isBlocking, float facingDot, CombatTuning tuning = null)
        {
            var t = tuning ?? CombatTuning.Default;
            if (!Blocks(isBlocking, facingDot, t.BlockFrontArcDot)) return amount;
            return CombatResolver.Reduced(amount, t.BlockReduction); // clamps the fraction to 0..1
        }

        /// <summary>How head-on a blow was: dot(forward, directionToAttacker) — 1 is dead ahead, 0 is
        /// the flank, negative is behind. Flattened to the horizontal plane, so a blow from above/below
        /// still counts as "in front" if it faces you. This is the geometry INPUT to
        /// <see cref="Blocked"/>, and it decides whether a shield actually stops a blow, so it lives
        /// here rather than in the MonoBehaviour that measures the positions (ADR: Unity is a shell).
        ///
        /// A source we can't locate (<paramref name="attackerPosition"/> null — e.g. an unattributed
        /// hit) is treated as frontal, so it isn't unfairly unblockable; the same default a degenerate
        /// (near-zero) facing or offset vector gets, for the same reason.</summary>
        public static float FacingDot(Vector3 selfPosition, Vector3 selfForward, Vector3? attackerPosition)
        {
            if (attackerPosition == null) return 1f;

            var toAttacker = attackerPosition.Value - selfPosition;
            toAttacker = new Vector3(toAttacker.X, 0f, toAttacker.Z); // a blow from above/below is still "in front" if it faces you
            if (toAttacker.LengthSquared() < 0.0001f) return 1f;

            var forward = new Vector3(selfForward.X, 0f, selfForward.Z);
            if (forward.LengthSquared() < 0.0001f) return 1f;

            return Vector3.Dot(Vector3.Normalize(forward), Vector3.Normalize(toAttacker));
        }
    }
}
