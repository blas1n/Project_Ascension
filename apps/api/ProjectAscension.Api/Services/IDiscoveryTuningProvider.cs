using ProjectAscension.SkillForge;

namespace ProjectAscension.Api.Services;

/// <summary>Supplies the current <see cref="DiscoveryTuning"/> to the rule engine.
/// Loaded fresh from the DB per call, so runtime balance edits apply immediately.</summary>
public interface IDiscoveryTuningProvider
{
    Task<DiscoveryTuning> GetAsync(CancellationToken ct = default);
}
