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
    int BudgetBase,
    double BudgetPerScore,
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
            ["Jump"] = 1,
            ["Dodge"] = 1,
            ["MeleeAttack"] = 2,
            ["RangedAttack"] = 2,
            ["ChargeAttack"] = 3,
            ["DodgeAttack"] = 3,
            ["ChargedAttack"] = 3, // a deliberate held shot — drives charge discoveries
        },
        new Dictionary<string, int>
        {
            // Environment / equipment / knowledge — notable factors that make a
            // behavior more significant (discovery.md 발견 생성 요소).
            ["waterfall"] = 10,
            ["ice_wall"] = 10,
            ["crystal_desert"] = 12,
            ["jungle"] = 8,
            ["sword"] = 4,
            ["bow"] = 4,
            ["pistol"] = 4,
            ["catalyst"] = 6,
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
        CombinationSynergy: 15,
        FuseWeight: 25,
        SequenceWeight: 15,
        ConcurrencyWeight: 12,
        ChainWeight: 6,
        FireThreshold: 100,
        BudgetBase: 8,
        BudgetPerScore: 0.18,
        BudgetMin: 16,
        BudgetMax: 64,
        UncommonScore: 120,
        RareScore: 150,
        EpicScore: 200,
        LegendaryScore: 250);
}
