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
                    ? "This play is OFFENSIVE → root trigger OnCast; build a Sequence: one Emit (its delivery is the shape — Projectile/Beam single-target, Burst/Nova area), then shape the attack with any of Damage (extra hit), Dot (a burn over time), Spread (hits extra targets — chain/pierce), Homing (the shot seeks), Control (Knockback/Slow/Stun). Mix these to make each attack distinct; don't just Emit+Damage every time."
                    : "Defensive/ambient → root trigger Continuous; effects are Ward.";

        return
$@"Compose a unique combat skill for a discovery as a deterministic EFFECT GRAPH.

Theme: {theme}
How the player fought: {play}
{steer}
Power budget: {budget.Total} (Emit/Damage/Dot cost (tier+1)*3, Impulse (tier+1)*4, Control (tier+1)*5, Ward (tier+1)*4, Spread (tier+1)*2, Homing 2; stay within budget).

Respond ONLY as JSON of this exact shape:
{{ ""trigger"": <TRIGGER>, ""effect"": <NODE> }}

TRIGGER is one of: OnCast, OnJumpInAir, OnDodge, OnWallContact, Continuous.
NODE is one of:
  {{""kind"":""Emit"",""delivery"":<Projectile|Beam|Burst|Nova>,""tier"":<0-3>}}
  {{""kind"":""Impulse"",""direction"":<Up|Forward|Aim>,""tier"":<0-3>}}
  {{""kind"":""Damage"",""tier"":<0-3>}}
  {{""kind"":""Dot"",""tier"":<0-3>,""duration"":<0-4>}}
  {{""kind"":""Spread"",""tier"":<0-3>}}
  {{""kind"":""Homing"",""tier"":<0-3>}}
  {{""kind"":""Control"",""effect"":<Knockback|Slow|Stun>,""tier"":<0-3>}}
  {{""kind"":""Ward"",""effect"":<Shield|Barrier|Heal|Leech>,""tier"":<0-3>}}
  {{""kind"":""Sequence"",""steps"":[<NODE>, ...]}}

Rules: exactly one top-level trigger; no trigger inside effect; tiers 0-3; keep it small (<=8 nodes);
match the trigger to the play above. COHERENCE (or the skill does nothing and is rejected):
a movement trigger (OnJumpInAir/OnWallContact/OnDodge-as-movement) MUST include an Impulse;
Continuous MUST include a Ward; OnCast MUST include a real effect (Emit/Damage/Dot/Control/Ward),
not only Impulse/Homing/Spread. Numbers/balance are the engine's — only choose structure and tiers.";
    }
}
