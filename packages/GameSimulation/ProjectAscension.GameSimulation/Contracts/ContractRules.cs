namespace ProjectAscension.GameSimulation.Contracts
{
    /// <summary>
    /// The deterministic rules of a contract's lifecycle (ADR: Unity is a shell) — progress
    /// clamping, deadline/delegation timers, and the accept/fail conditions. Pure and primitive-in
    /// (no ContractInstance, no MonoBehaviour), so the contract loop is headless-testable. The Unity
    /// ContractService owns the state + event wiring and calls these; it makes no decisions itself.
    /// </summary>
    public static class ContractRules
    {
        /// <summary>Progress after crediting <paramref name="amount"/>, never past the target.</summary>
        public static int ClampedProgress(int current, int amount, int target)
        {
            int next = current + amount;
            return next > target ? target : next;
        }

        /// <summary>Whether the objective is met.</summary>
        public static bool IsComplete(int progress, int target) => progress >= target;

        /// <summary>The player's standing meets the contract's requirement.</summary>
        public static bool CanAccept(int reputation, int minReputation) => reputation >= minReputation;

        /// <summary>Tick a countdown; returns the new remaining time and whether it just elapsed.</summary>
        public static (float Remaining, bool Elapsed) TickTimer(float remaining, float dt)
        {
            float next = remaining - dt;
            return (next, next <= 0f);
        }

        /// <summary>A contract fails on death only if it opts in AND isn't already complete — so a
        /// death during a non-death-fail contract (e.g. the delegation tutorial) does NOT fail it.</summary>
        public static bool FailsOnDeath(bool failOnDeath, bool isComplete) => failOnDeath && !isComplete;

        /// <summary>Whether a timeout-failing contract is currently eligible to expire (opted in and
        /// not yet complete); the caller then ticks the timer with <see cref="TickTimer"/>.</summary>
        public static bool CanExpire(bool failOnTimeout, bool isComplete) => failOnTimeout && !isComplete;

        /// <summary>The standing lost on a contract failure: the contract's reward reputation, clamped
        /// so a player can never be pushed below zero standing by a single failure.</summary>
        public static int ReputationPenalty(int currentReputation, int rewardReputation)
            => currentReputation < rewardReputation ? currentReputation : rewardReputation;
    }
}
