namespace ProjectAscension.SkillForge;

/// <summary>
/// The tunable knobs the discovery rule engine scores against — behavior weights,
/// scoring coefficients, the power-budget curve, and rarity bands. SkillForge stays
/// pure: the host loads these (from the DB, at runtime) and passes them in, so
/// balance is data-driven and server-authoritative (ADR 0002), never hard-coded.
/// </summary>
public sealed record DiscoveryTuning(
    IReadOnlyDictionary<string, int> BehaviorWeights,
    IReadOnlyDictionary<string, int> FactorWeights,
    int DefaultBehaviorWeight,
    int DefaultFactorWeight,
    int KnowledgeDepthWeight,
    int PersistenceWeight,
    int CombinationSynergy,
    // The composition grammar's operators (ADR 0009), scored by prefix. Ordered by how deliberate the
    // act is: fusing two things in an instant is the hardest, and repetition the easiest. You do not
    // stumble into weaving your catalyst through your gunshot — and spamming one hand must never get
    // you there.
    int FuseWeight,        // Fuse:a>b  — almost the same instant
    int SequenceWeight,    // Seq:a>b   — one act into the next
    int ConcurrencyWeight, // While:a@q — done while airborne / charged / blocking
    int ChainWeight,       // Chain:a   — done again and again
    int FireThreshold,
    // The Nth discovery in the SAME space costs exponentially more (ADR 0010). The first is easy; the
    // fifth is ten times harder. Repeating one act runs out of road — you must play differently, or
    // compose better (ADR 0009). Grinding is made to exhaust itself.
    int BudgetBase,
    // Power rises LOGARITHMICALLY while cost rises exponentially (ADR 0010). A 60x score buys a 1.6x
    // budget. Getting stronger is never forbidden — only made steadily more expensive.
    double BudgetGrowth,
    int BudgetMin,
    int BudgetMax,
    int UncommonScore,
    int RareScore,
    int EpicScore,
    int LegendaryScore)
{
    /// <summary>A baseline used by tests and as a safe fallback when the DB has no
    /// tuning row yet. Mirrors the seeded defaults.</summary>
    public static DiscoveryTuning Default { get; } = new(
        new Dictionary<string, int>
        {
            // Raw verbs only (BehaviorKind) — composites (Fuse:/Seq:/While:/Chain:) are scored by
            // PREFIX (ADR 0009), never by a dictionary row. "ChargeAttack"/"ChargedAttack" used to be
            // seeded here, but a charged shot is now a While:...@charged quality on the act stream, not
            // its own BehaviorKind — those two rows were leftovers from before the grammar refactor and
            // could never be hit. Removed from the DB by the CompositionGrammar migration; removed here
            // to match (this dictionary mirrors the DB seed).
            ["Jump"] = 1,
            ["MeleeAttack"] = 2,
            ["RangedAttack"] = 2,
        },
        new Dictionary<string, int>
        {
            // Environment / equipment / knowledge — notable factors that make a
            // behavior more significant (discovery.md 발견 생성 요소).
            ["waterfall"] = 10,
            ["ice_wall"] = 10,
            ["crystal_desert"] = 12,
            ["jungle"] = 8,
            // Equipment-category tags EquipmentTags/SkillBinding actually emit (ADR 0005/0011), not the
            // starter weapon names ("sword"/"pistol"/"catalyst") that used to be seeded here and never
            // matched anything the game sends. Weights carried over 1:1 (Sword→melee, Pistol→firearm,
            // Catalyst→arcane); Bow already matched.
            ["melee"] = 4,
            ["bow"] = 4,
            ["firearm"] = 4,
            ["arcane"] = 6,
            ["fire"] = 8,
            ["compression"] = 8,
            ["wind"] = 8,
            // Monsters as discovery catalysts (combat-framework: 몬스터는 발견의 촉매).
            ["monster:melee"] = 6,
            ["monster:ranged"] = 8,
            ["monster:elite"] = 14,
        },
        DefaultBehaviorWeight: 1,
        DefaultFactorWeight: 0,
        KnowledgeDepthWeight: 12,
        PersistenceWeight: 5,
        // Trimmed from 15 (DiscoveryScarcity, ADR 0010): merely touching a few distinct behaviour/
        // factor KINDS in one window (an attack, a fuse, a chain — three "kinds" from one brief burst
        // of play) was worth as much as the fuse itself. Variety still pays; it no longer dominates.
        CombinationSynergy: 10,
        FuseWeight: 25,
        SequenceWeight: 15,
        ConcurrencyWeight: 12,
        ChainWeight: 6,
        // Raised from 100 (DiscoveryScarcity, ADR 0010): one spell cast fused into a short mag-dump —
        // "한번 썼는데 발견이 쏟아진다" — scored ~100-120 under the old numbers, right at or over the old
        // threshold. 200 puts that same brief burst at roughly half of what a Common now costs, so the
        // first rung is earned by sustained play (still reachable by grinding, per ADR 0010 §1-c), not
        // handed out by one twitch.
        FireThreshold: 200,
        BudgetBase: 6,
        BudgetGrowth: 2.4,
        BudgetMin: 10,
        BudgetMax: 40, // a full 8-effect graph — significance buys BREADTH, and then saturates
                       // Same ×1.5 exponential spacing as before (ADR 0010 §1-a), rebased off the new FireThreshold.
        UncommonScore: 300,
        RareScore: 450,
        EpicScore: 675,
        LegendaryScore: 1013);
}
