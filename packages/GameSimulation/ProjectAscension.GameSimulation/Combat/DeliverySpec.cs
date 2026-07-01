namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>Where a skill's delivery originates.</summary>
    public enum DeliveryOrigin { Muzzle, AimPoint, Self, Placed }

    /// <summary>How the delivery moves (Seek/Orbit are reserved for later).</summary>
    public enum DeliveryMotion { None, Projectile, Seek, Orbit }

    /// <summary>When it resolves its effect (Proximity/Channel are reserved for later).</summary>
    public enum DeliveryTrigger { OnImpact, Periodic, Proximity, Channel }

    /// <summary>The footprint it resolves over (Cone/Line are reserved for later).</summary>
    public enum DeliveryShape { Point, Sphere, Cone, Line }

    /// <summary>
    /// HOW a skill manifests in the world — the delivery, kept separate from its EFFECT
    /// (the numbers, owned by <see cref="SkillResolver"/>). This is a PARAMETRIC model: a
    /// manifestation is a point in the axis-space (origin × motion × trigger × shape +
    /// params), so new kinds — a turret, a lingering zone, a summon — are new axis
    /// COMBINATIONS (data), not new code. The developer grows the axes (few, orthogonal,
    /// slow), not the kinds (many, combinatorial).
    ///
    /// The spec is just data, so its SOURCE is pluggable: today it is INFERRED from the
    /// skill's primitives (<see cref="DeliveryInference"/>); later the AI composes it
    /// directly; or — the architectural door we leave open — a sandboxed, statically
    /// verified deterministic DSL emits it (or a richer behavior program). The executor
    /// only reads the spec, so any of these slot in without touching the core. Living in
    /// the shared simulation also lets the server run delivery deterministically (ADR 0006).
    /// </summary>
    public sealed record DeliverySpec(
        DeliveryOrigin Origin,
        DeliveryMotion Motion,
        DeliveryTrigger Trigger,
        DeliveryShape Shape,
        float Speed,        // projectile speed when Motion == Projectile
        float Gravity,      // projectile drop
        float Range,        // how far it reaches
        float Radius,       // gather/area radius (also the resolve catch radius)
        float Lifetime,     // 0 = instant; > 0 = persists this many seconds; < 0 = until destroyed
        float TickInterval) // cadence when Trigger == Periodic
    {
        /// <summary>A one-shot delivery that resolves once on impact (no persistence).</summary>
        public bool IsInstant => Lifetime <= 0f && Trigger == DeliveryTrigger.OnImpact;

        /// <summary>A persistent delivery (zone / turret / summon) that lives in the world
        /// and resolves over time — reserved for later; today's inference never produces it.</summary>
        public bool IsPersistent => Lifetime != 0f || Trigger == DeliveryTrigger.Periodic;
    }

    /// <summary>
    /// Derives a <see cref="DeliverySpec"/> from a composed skill's primitives — the
    /// current spec SOURCE. Offensive primitives map to instant deliveries: a Projectile
    /// flies, an Area lands at the aim point, a Beam (or anything else) is a hitscan line.
    /// When the AI later composes delivery axes directly (or a DSL emits them), this is
    /// simply replaced as the source; the executor is unchanged.
    /// </summary>
    /// <summary>
    /// Maps an AI-composed delivery STYLE (the composer's vocabulary — "projectile" / "beam"
    /// / "burst", see DeliveryStyleCatalog) to a concrete <see cref="DeliverySpec"/>, with the
    /// numbers from the DB-driven tuning. This is the primary source once the AI composes the
    /// delivery; <see cref="DeliveryInference"/> remains the fallback when a skill has no
    /// composed style (offline/stub/legacy). Adding a style here + to the catalog extends what
    /// the AI can compose without touching the executor.
    /// </summary>
    public static class DeliveryStyles
    {
        public static DeliverySpec? ForStyle(string? style, CombatTuning tuning)
        {
            var t = tuning ?? CombatTuning.Default;
            switch ((style ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "projectile":
                    return new DeliverySpec(DeliveryOrigin.Muzzle, DeliveryMotion.Projectile, DeliveryTrigger.OnImpact, DeliveryShape.Sphere,
                        Speed: t.DeliveryProjectileSpeed, Gravity: t.DeliveryProjectileGravity, Range: t.DeliveryRange, Radius: t.DeliveryHitscanRadius, Lifetime: 0f, TickInterval: 0f);
                case "arc":
                    // A lobbed projectile — reuses the projectile executor with gravity so it
                    // arcs and drops (thrown on the move).
                    return new DeliverySpec(DeliveryOrigin.Muzzle, DeliveryMotion.Projectile, DeliveryTrigger.OnImpact, DeliveryShape.Sphere,
                        Speed: t.DeliveryProjectileSpeed * 0.85f, Gravity: 18f, Range: t.DeliveryRange, Radius: t.DeliveryHitscanRadius, Lifetime: 0f, TickInterval: 0f);
                case "beam":
                    return new DeliverySpec(DeliveryOrigin.Muzzle, DeliveryMotion.None, DeliveryTrigger.OnImpact, DeliveryShape.Line,
                        Speed: 0f, Gravity: 0f, Range: t.DeliveryRange, Radius: t.DeliveryHitscanRadius, Lifetime: 0f, TickInterval: 0f);
                case "burst":
                    return new DeliverySpec(DeliveryOrigin.AimPoint, DeliveryMotion.None, DeliveryTrigger.OnImpact, DeliveryShape.Sphere,
                        Speed: 0f, Gravity: 0f, Range: t.DeliveryRange, Radius: t.DeliveryAreaRadius, Lifetime: 0f, TickInterval: 0f);
                case "nova":
                    // Erupts around the caster (weaving/leaping while charging).
                    return new DeliverySpec(DeliveryOrigin.Self, DeliveryMotion.None, DeliveryTrigger.OnImpact, DeliveryShape.Sphere,
                        Speed: 0f, Gravity: 0f, Range: 0f, Radius: t.DeliveryAreaRadius * 1.2f, Lifetime: 0f, TickInterval: 0f);
                default:
                    return null; // unknown/none → caller falls back to DeliveryInference
            }
        }
    }

    public static class DeliveryInference
    {
        /// <summary>Derive the delivery from the skill's primitives. The discrete axes are
        /// the manifestation concept; the numbers come from <paramref name="tuning"/>
        /// (DB-driven, deterministic — never hard-coded, ADR 0002).</summary>
        public static DeliverySpec From(Skill skill, CombatTuning tuning)
        {
            var t = tuning ?? CombatTuning.Default;

            if (Has(skill, SkillPrimitiveKind.Projectile))
                return new DeliverySpec(DeliveryOrigin.Muzzle, DeliveryMotion.Projectile, DeliveryTrigger.OnImpact, DeliveryShape.Sphere,
                    Speed: t.DeliveryProjectileSpeed, Gravity: t.DeliveryProjectileGravity, Range: t.DeliveryRange, Radius: t.DeliveryHitscanRadius, Lifetime: 0f, TickInterval: 0f);

            if (Has(skill, SkillPrimitiveKind.Area) && !Has(skill, SkillPrimitiveKind.Beam))
                return new DeliverySpec(DeliveryOrigin.AimPoint, DeliveryMotion.None, DeliveryTrigger.OnImpact, DeliveryShape.Sphere,
                    Speed: 0f, Gravity: 0f, Range: t.DeliveryRange, Radius: t.DeliveryAreaRadius, Lifetime: 0f, TickInterval: 0f);

            // Beam or default → an instant hitscan line resolving at the first thing hit.
            return new DeliverySpec(DeliveryOrigin.Muzzle, DeliveryMotion.None, DeliveryTrigger.OnImpact, DeliveryShape.Line,
                Speed: 0f, Gravity: 0f, Range: t.DeliveryRange, Radius: t.DeliveryHitscanRadius, Lifetime: 0f, TickInterval: 0f);
        }

        private static bool Has(Skill skill, SkillPrimitiveKind kind)
        {
            foreach (var p in skill.Primitives)
                if (p.Kind == kind) return true;
            return false;
        }
    }
}
