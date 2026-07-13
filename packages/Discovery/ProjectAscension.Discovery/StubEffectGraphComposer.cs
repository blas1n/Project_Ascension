using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectAscension.SkillForge;

/// <summary>
/// Deterministic, offline effect-graph composer — used when no LLM is configured (CI, tests) and
/// as the reproducible baseline. It mirrors <see cref="EffectGraphPrompt"/>'s steering: it reads
/// how the player fought and emits the matching root trigger + a stock effect (movement → an
/// Impulse under a movement trigger; offensive → OnCast + Emit/Damage; else a Continuous Ward).
/// No AI, but the SAME graph vocabulary — so the runtime interpreter can be built and tested
/// (ADR 0007 Phase 2) before the LLM path is wired.
/// </summary>
public sealed class StubEffectGraphComposer : IEffectGraphComposer
{
    public Task<SkillGraphComposition?> ComposeAsync(EffectGraphRequest request, CancellationToken ct = default)
    {
        var profile = request.Profile ?? new List<BehaviorWeight>();
        int attacks = profile.Where(b => b.Behavior is "RangedAttack" or "MeleeAttack" or "ChargedAttack").Sum(b => b.Count);
        int mobility = profile.Where(b => b.Behavior == "Jump").Sum(b => b.Count);

        EffectNode graph;
        if (mobility * 2 > attacks * 3)
        {
            // Movement-dominant → a movement capability: an extra jump in the air (double jump).
            // No engine special-case — a trigger + an upward impulse.
            graph = new Trigger(TriggerKind.OnJumpInAir, new Impulse(ImpulseDirection.Up, 1));
        }
        else if (attacks > 0)
        {
            graph = new Trigger(TriggerKind.OnCast,
                new Sequence(new EffectNode[] { new Emit(EmitDelivery.Projectile, 1), new Damage(1) }));
        }
        else
        {
            graph = new Trigger(TriggerKind.Continuous, new Ward(WardEffect.Shield, 1));
        }

        // Guard the budget deterministically (the engine owns numbers, ADR 0002): if the stock
        // graph somehow exceeds it, drop to the cheapest valid shape rather than emit over-budget.
        if (!EffectGraphValidator.Validate(graph, request.Budget).IsValid)
            graph = new Trigger(TriggerKind.OnCast, new Damage(0));

        // Deterministic name/description from the theme (offline/CI has no LLM for flavor).
        string theme = string.IsNullOrWhiteSpace(request.Theme) ? "Discovery" : request.Theme.Trim();
        string name = char.ToUpperInvariant(theme[0]) + (theme.Length > 1 ? theme.Substring(1) : "");
        var comp = new SkillGraphComposition(name, $"A discovered technique: {theme}.", graph);
        return Task.FromResult<SkillGraphComposition?>(comp);
    }
}
