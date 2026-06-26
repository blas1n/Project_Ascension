#nullable enable

namespace ProjectAscension.Domain.Entities
{
    /// <summary>
    /// Server-authoritative difficulty weight for a player behavior, consumed by the
    /// discovery trigger's significance scoring (ADR 0002 — numbers are
    /// server-owned). Stored as a row so balance designers can add behaviors or
    /// retune weights at runtime, without a redeploy.
    /// </summary>
    public class BehaviorWeight
    {
        public string Behavior { get; set; } = string.Empty; // primary key
        public int Weight { get; set; }
    }
}
