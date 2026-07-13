namespace ProjectAscension.GameSimulation.Discovery
{
    /// <summary>Trackable player behaviors that can drive discoveries.</summary>
    public enum BehaviorKind
    {
        // The RAW verbs only: what was done, and how many times. Every composite — an
        // air attack, a chained jump, a weapon fusion — used to be a member here, and each one needed a
        // bespoke observer to produce it. They are now sentences in the composition grammar (ADR 0009),
        // which means combinations nobody enumerated still come out.
        Jump,
        MeleeAttack,
        RangedAttack,
    }
}
