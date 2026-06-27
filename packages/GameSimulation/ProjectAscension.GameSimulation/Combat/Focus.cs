namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Pure focus state — the resource for skills and special actions
    /// (combat-framework.md 전투 자원: 집중력, not mana; every weapon line uses it).
    /// Deterministic and identical on server and client, like <see cref="Health"/>.
    /// </summary>
    public record Focus(float Current, float Max)
    {
        public static Focus Full(float max) => new(max, max);

        public bool Has(float amount) => Current >= amount;
    }
}
