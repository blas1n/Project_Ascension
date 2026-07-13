using ProjectAscension.Domain.Interfaces;
using ProjectAscension.SkillForge;

namespace ProjectAscension.Api.Services;

/// <summary>Builds the pure <see cref="DiscoveryTuning"/> from the DB rows each time
/// it is asked, so a balance designer's edit to the tuning row or behavior weights
/// takes effect on the very next discovery evaluation (no redeploy, no restart).
/// Falls back to <see cref="DiscoveryTuning.Default"/> when the DB has no tuning row
/// yet.</summary>
public class DiscoveryTuningProvider : IDiscoveryTuningProvider
{
    private readonly IDiscoveryTuningRepository _repo;
    public DiscoveryTuningProvider(IDiscoveryTuningRepository repo) => _repo = repo;

    public async Task<DiscoveryTuning> GetAsync(CancellationToken ct = default)
    {
        var settings = await _repo.GetSettingsAsync(ct);
        if (settings is null) return DiscoveryTuning.Default;

        var weights = await _repo.GetBehaviorWeightsAsync(ct);
        var behaviorWeights = weights.ToDictionary(w => w.Behavior, w => w.Weight);
        var factors = await _repo.GetFactorWeightsAsync(ct);
        var factorWeights = factors.ToDictionary(f => f.Key, f => f.Weight);

        return new DiscoveryTuning(
            behaviorWeights,
            factorWeights,
            settings.DefaultBehaviorWeight,
            settings.DefaultFactorWeight,
            settings.KnowledgeDepthWeight,
            settings.PersistenceWeight,
            settings.CombinationSynergy,
            settings.FuseWeight,
            settings.SequenceWeight,
            settings.ConcurrencyWeight,
            settings.ChainWeight,
            settings.FireThreshold,
            settings.BudgetBase,
            settings.BudgetGrowth,
            settings.BudgetMin,
            settings.BudgetMax,
            settings.UncommonScore,
            settings.RareScore,
            settings.EpicScore,
            settings.LegendaryScore);
    }
}
