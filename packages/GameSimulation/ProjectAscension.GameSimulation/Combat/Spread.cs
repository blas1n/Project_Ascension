namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// A weapon's current bullet spread (cone half-angle, degrees) plus its bounds —
    /// the accuracy/grouping state. Sustained fire blooms it toward <see cref="Max"/>;
    /// not firing recovers it toward <see cref="Min"/>. A pure value so the bloom math
    /// is deterministic and testable. The cone SAMPLE (which direction a given shot
    /// actually deviates) is also deterministic — see <see cref="SpreadRules.Deviation"/> —
    /// because it decides whether the shot hits, and that decision cannot live only on
    /// the renderer (ADR: Unity is a shell).
    /// </summary>
    public record Spread(float Current, float Min, float Max)
    {
        public static Spread From(float min, float max) => new(min, min, max);
    }
}
