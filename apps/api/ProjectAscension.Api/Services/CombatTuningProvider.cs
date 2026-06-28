using ProjectAscension.Domain.Interfaces;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Api.Services;

public interface ICombatTuningProvider
{
    Task<CombatTuning> GetAsync(CancellationToken ct = default);
}

/// <summary>Builds the pure <see cref="CombatTuning"/> from the DB row each time it is
/// asked, so a balance designer's edit takes effect on the next read (no redeploy).
/// Falls back to <see cref="CombatTuning.Default"/> when the DB has no row yet.</summary>
public class CombatTuningProvider : ICombatTuningProvider
{
    private readonly ICombatTuningRepository _repo;
    public CombatTuningProvider(ICombatTuningRepository repo) => _repo = repo;

    public async Task<CombatTuning> GetAsync(CancellationToken ct = default)
    {
        var s = await _repo.GetSettingsAsync(ct);
        if (s is null) return CombatTuning.Default;

        return new CombatTuning(
            s.ProjectileDamage, s.BeamDamage, s.AreaDamage, s.DotDamagePerTick, s.SpreadFalloff,
            s.BaseDotTicks, s.ShieldPerMagnitude, s.DashPerMagnitude, s.LeechFractionPerMagnitude,
            s.ControlDurationPerMagnitude, s.PassiveShieldReduction, s.PassiveBarrierReduction,
            s.PassiveLeech, s.FocusCostPerPoint,
            s.SlowPerMagnitude, s.KnockbackPerMagnitude, s.ChargedAttackThreshold);
    }
}
