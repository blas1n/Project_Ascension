using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.World
{
    /// <summary>One thing in the world the player could press [F] on right now: its identity, how far
    /// away it is, and how far away it CAN be interacted with. Reach travels WITH the candidate rather
    /// than being a single global radius — a contract board should be readable from across the square,
    /// while a dropped item only when you're standing on it. Distance/reach are measured by the shell
    /// (XZ distance, a serialized field); this struct carries no Unity types so selection is
    /// headless-testable (ADR: Unity is a shell).</summary>
    public readonly struct InteractCandidate
    {
        public readonly int Id;
        public readonly float Distance;
        public readonly float Reach;

        public InteractCandidate(int Id, float Distance, float Reach)
        {
            this.Id = Id;
            this.Distance = Distance;
            this.Reach = Reach;
        }
    }

    /// <summary>
    /// Picks which interactable a press of [F] should hit, out of every candidate in the world this
    /// frame. Never opens or triggers anything itself — the shell reads the winning Id back and decides
    /// what that means (open a panel, load a scene, ...). Kept pure so "which thing wins when several
    /// are in range" is verified without Unity, the same way <c>MonsterAi</c>/<c>WeaponFireRules</c> are.
    /// </summary>
    public static class InteractionRules
    {
        /// <summary>The nearest candidate that is within ITS OWN reach, or -1 if none qualify. A
        /// candidate whose distance exceeds its own reach never wins, even if it is the closest thing
        /// in the list overall (being "closest" doesn't matter if you still can't reach it). Ties
        /// (equal distance) resolve to the lowest Id so the pick is deterministic rather than depending
        /// on the shell's registration/iteration order, which is not guaranteed stable frame to frame.</summary>
        public static int Best(IReadOnlyList<InteractCandidate> candidates)
        {
            int bestId = -1;
            float bestDistance = float.PositiveInfinity;

            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                if (c.Distance > c.Reach) continue; // out of its own reach — never a winner

                if (c.Distance < bestDistance || (c.Distance == bestDistance && c.Id < bestId))
                {
                    bestDistance = c.Distance;
                    bestId = c.Id;
                }
            }

            return bestId;
        }
    }
}
