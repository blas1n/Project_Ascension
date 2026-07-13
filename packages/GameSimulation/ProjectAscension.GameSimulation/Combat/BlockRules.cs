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
    }
}
