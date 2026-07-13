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
        AirAttack,     // struck while airborne — the doc's training example (공중 공격)
        RepeatedJump,  // a deliberate chain of jumps (반복 점프)
    }
}
