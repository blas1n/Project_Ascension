using System.Collections.Generic;
using System.Linq;

namespace ProjectAscension.SkillForge;

/// <summary>
/// Builds the prompt that asks the model to compose a skill as an EFFECT GRAPH (ADR 0007) — the
/// model owns the STRUCTURE (which trigger, which effects, in what order); the engine owns the
/// numbers (tiers → tuning) and executes deterministically. The graph generalizes past a flat
/// primitive list: a new mechanic (e.g. wall-climb) is just a trigger + a stock effect.
/// </summary>
public static class EffectGraphPrompt
{
    public static string Build(string theme, IReadOnlyList<BehaviorWeight> profile, PowerBudget budget)
    {
        int attacks = profile?.Where(b => b.Behavior is "RangedAttack" or "MeleeAttack" or "ChargedAttack").Sum(b => b.Count) ?? 0;
        int mobility = profile?.Where(b => b.Behavior is "Jump" or "Dodge").Sum(b => b.Count) ?? 0;
        string play = profile is null || profile.Count == 0
            ? "no clear pattern"
            : string.Join(", ", profile.OrderByDescending(b => b.Count).Select(b => $"{b.Behavior}:{b.Count}"));

        string steer =
            mobility * 2 > attacks * 3
                ? "This play is MOVEMENT-dominated → root trigger OnJumpInAir or OnDodge (a movement capability, e.g. a double jump = OnJumpInAir + Impulse Up), or OnWallContact for a wall-climb; effects are Impulse only, no offense."
                : attacks > 0
                    ? "This play is OFFENSIVE → root trigger OnCast; effects are Emit + Damage (optionally one Control)."
                    : "Defensive/ambient → root trigger Continuous; effects are Ward.";

        return
$@"Compose a unique combat skill for a discovery as a deterministic EFFECT GRAPH.

Theme: {theme}
How the player fought: {play}
{steer}
Power budget: {budget.Total} (Emit/Damage cost (tier+1)*3, Impulse (tier+1)*4, Control (tier+1)*5, Ward (tier+1)*4; stay within budget).

Respond ONLY as JSON of this exact shape:
{{ ""trigger"": <TRIGGER>, ""effect"": <NODE> }}

TRIGGER is one of: OnCast, OnJumpInAir, OnDodge, OnHit, OnWallContact, Continuous.
NODE is one of:
  {{""kind"":""Emit"",""delivery"":<Projectile|Beam|Burst|Nova>,""tier"":<0-3>}}
  {{""kind"":""Impulse"",""direction"":<Up|Forward|Aim>,""tier"":<0-3>}}
  {{""kind"":""Damage"",""tier"":<0-3>}}
  {{""kind"":""Control"",""effect"":<Knockback|Slow|Stun>,""tier"":<0-3>}}
  {{""kind"":""Ward"",""effect"":<Shield|Barrier|Heal|Leech>,""tier"":<0-3>}}
  {{""kind"":""Sequence"",""steps"":[<NODE>, ...]}}

Rules: exactly one top-level trigger; no trigger inside effect; tiers 0-3; keep it small (<=8 nodes);
match the trigger to the play above. Numbers/balance are the engine's — only choose structure and tiers.";
    }
}
