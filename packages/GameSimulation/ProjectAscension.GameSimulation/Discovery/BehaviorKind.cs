namespace ProjectAscension.GameSimulation.Discovery
{
    /// <summary>Trackable player behaviors that can drive discoveries.</summary>
    public enum BehaviorKind
    {
        Jump,
        Dodge,
        MeleeAttack,
        RangedAttack,
        DodgeAttack,
        ChargedAttack, // a held/charged shot (e.g. a full bow draw) — drives charge discoveries
    }
}
