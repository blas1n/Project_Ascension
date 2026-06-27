namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Pure health state. Shared by player and monsters; the server owns it
    /// authoritatively and replicates it to clients (ADR 0006).
    /// </summary>
    public record Health(float Current, float Max)
    {
        public bool IsDead => Current <= 0f;

        public static Health Full(float max) => new(max, max);
    }
}
