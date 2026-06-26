namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// The combat-side mirror of the discovery engine's effect primitives. Names match
    /// the server's <c>ProjectAscension.SkillForge.PrimitiveKind</c> exactly, so a
    /// composed skill's primitives parse straight into executable combat effects — but
    /// this lives in the Unity-compiled simulation, which cannot reference the
    /// server-only SkillForge package.
    /// </summary>
    public enum SkillPrimitiveKind
    {
        Projectile,
        Homing,
        Pierce,
        Area,
        DamageOverTime,
        Chain,
        Fork,
        Beam,
        Knockback,
        Slow,
        Stun,
        Dash,
        Blink,
        Shield,
        Barrier,
        Leech,
    }
}
