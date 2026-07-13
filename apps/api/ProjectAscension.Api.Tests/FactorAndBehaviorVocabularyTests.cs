using ProjectAscension.Api.Data.Configurations;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Discovery;
using ProjectAscension.SkillForge;

namespace ProjectAscension.Api.Tests;

/// <summary>
/// Pins the invariant that let "sword"/"pistol"/"catalyst" (equipment factor keys the game never
/// emits — EquipmentTags/SkillBinding only ever send "melee"/"firearm"/"bow"/"arcane") and the
/// pre-ADR-0009 "ChargeAttack"/"ChargedAttack" leftovers (superseded by the While:...@charged
/// grammar, already dropped from the DB by the CompositionGrammar migration) sit in the tuning
/// tables unnoticed: a seeded row that names a key nothing can ever send is a LIE in the balance
/// table — it looks tuned, and does nothing.
///
/// Checks both the DB seed (FactorWeightConfiguration.Seed / BehaviorWeightConfiguration.Seed) and
/// DiscoveryTuning.Default — its documented in-code mirror/fallback (see DiscoveryTuning.cs) — so
/// neither can drift again without failing CI.
/// </summary>
public class FactorAndBehaviorVocabularyTests
{
    // The only Equipment-category tokens EquipmentTags (client) / SkillBinding (server) can ever
    // produce (ADR 0005, revised ADR 0011). Cross-checked here via SkillBinding.WeaponTags, the
    // server-side copy of the same vocabulary that already keys skill binding.
    private static readonly HashSet<string> EquipmentVocabulary = new(SkillBinding.WeaponTags);

    // The only raw verbs BehaviorKind (and therefore DiscoveryReporter) can ever emit. Composite
    // behaviours (Fuse:/Seq:/While:/Chain:) are scored by PREFIX (ADR 0009) and never appear as a
    // literal BehaviorWeight row.
    private static readonly HashSet<string> BehaviorVocabulary = new(Enum.GetNames(typeof(BehaviorKind)));

    [Fact]
    public void SeededEquipmentFactorKeys_MatchTheGamesEquipmentVocabulary()
    {
        var deadRows = FactorWeightConfiguration.Seed
            .Where(f => f.Category == "Equipment" && !EquipmentVocabulary.Contains(f.Key))
            .Select(f => f.Key)
            .ToList();

        Assert.True(deadRows.Count == 0,
            $"FactorWeight row(s) [{string.Join(", ", deadRows)}] are keyed 'Equipment' but are never " +
            $"emitted by EquipmentTags/SkillBinding (which only ever send: {string.Join(", ", EquipmentVocabulary)}) " +
            "— dead weight in the balance table.");
    }

    [Fact]
    public void SeededBehaviorWeightKeys_MatchTheGamesBehaviorVocabulary()
    {
        var deadRows = BehaviorWeightConfiguration.Seed
            .Where(b => !BehaviorVocabulary.Contains(b.Behavior))
            .Select(b => b.Behavior)
            .ToList();

        Assert.True(deadRows.Count == 0,
            $"BehaviorWeight row(s) [{string.Join(", ", deadRows)}] are never emitted by BehaviorKind " +
            $"(only: {string.Join(", ", BehaviorVocabulary)}) — dead weight in the balance table.");
    }

    [Fact]
    public void DefaultFactorWeights_MirrorTheDbSeedKeys()
    {
        // DiscoveryTuning.Default is documented as mirroring the DB seed, and is the fallback used
        // when no tuning row exists yet (DiscoveryTuningProvider) and in tests. If it drifts from the
        // DB seed — as it did when "ChargeAttack"/"ChargedAttack" were dropped from the DB but left
        // behind here — a fallback/test run scores against keys production never would.
        var dbKeys = FactorWeightConfiguration.Seed.Select(f => f.Key).OrderBy(k => k).ToList();
        var defaultKeys = DiscoveryTuning.Default.FactorWeights.Keys.OrderBy(k => k).ToList();

        Assert.Equal(dbKeys, defaultKeys);
    }

    [Fact]
    public void DefaultBehaviorWeights_MirrorTheDbSeedKeys()
    {
        var dbKeys = BehaviorWeightConfiguration.Seed.Select(b => b.Behavior).OrderBy(k => k).ToList();
        var defaultKeys = DiscoveryTuning.Default.BehaviorWeights.Keys.OrderBy(k => k).ToList();

        Assert.Equal(dbKeys, defaultKeys);
    }
}
