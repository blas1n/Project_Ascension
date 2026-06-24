namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Pure health state. Shared by player and monsters so damage resolution is
    /// deterministic and identical on server and client.
    /// </summary>
    public record Health(float Current, float Max)
    {
        public bool IsDead => Current <= 0f;

        public static Health Full(float max) => new(max, max);
    }
}
