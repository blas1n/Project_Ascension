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
        int jumps = profile?.Where(b => b.Behavior == "Jump").Sum(b => b.Count) ?? 0;
        int dodges = profile?.Where(b => b.Behavior == "Dodge").Sum(b => b.Count) ?? 0;
        int mobility = jumps + dodges;
        string play = profile is null || profile.Count == 0
            ? "no clear pattern"
            : string.Join(", ", profile.OrderByDescending(b => b.Count).Select(b => $"{b.Behavior}:{b.Count}"));

        // A FUSION the player actually performed (ADR 0008) — two hands used as one act. This is the
        // ONLY evidence that a skill should be a synthesis. Carrying a catalyst is not fusing with it.
        var fusions = profile?.Where(b => b.Behavior.StartsWith(TriggerEvaluator.SynthesisPrefix, StringComparison.Ordinal))
                              .OrderByDescending(b => b.Count).ToList();
        string fusionSteer = fusions is null || fusions.Count == 0
            ? "NO FUSION was performed. The player never used their two hands as one act. Do NOT invent a hybrid/imbued skill (no \"flaming bullet\", no \"frost blade\") merely because they were CARRYING two things — equipment tags say what they held, never what they combined. Compose from what they actually DID."
            : "FUSION PERFORMED — this is the heart of the skill. " +
              string.Join("; ", fusions.Select(f =>
              {
                  var pair = f.Behavior.Substring(TriggerEvaluator.SynthesisPrefix.Length).Split('>');
                  string primer = pair.Length > 0 ? pair[0] : "?";
                  string delivery = pair.Length > 1 ? pair[1] : "?";
                  return $"the player wove {primer} INTO {delivery} ({f.Count}x) — the skill IS that fusion: {delivery} is the vehicle and {primer} is what it now carries";
              })) +
              ". The ORDER matters: X>Y means X was primed and Y delivered it. Compose the graph so the delivery carries the primer's nature (e.g. arcane>firearm = a shot that carries the arcane; firearm>arcane = the arcane detonating what the shot left behind).";

        string themeLower = (theme ?? string.Empty).ToLowerInvariant();
        bool defensiveTheme = themeLower.Contains("ward") || themeLower.Contains("shield") || themeLower.Contains("guard")
                              || themeLower.Contains("barrier") || themeLower.Contains("protect") || themeLower.Contains("aegis");
        bool wallTheme = themeLower.Contains("wall") || themeLower.Contains("climb") || themeLower.Contains("scale")
                         || themeLower.Contains("cliff");

        // Pick ONE trigger prescriptively so different plays/themes yield different structures
        // (the discovery variety promise), not the same movement trigger every time.
        string steer;
        if (defensiveTheme && attacks == 0)
            steer = "This is a DEFENSIVE ward → root trigger Continuous; effect is a Ward (Shield/Barrier reduce incoming damage, Leech sustains). No offense, no movement.";
        else if (mobility * 2 > attacks * 3)
        {
            string moveTrigger = wallTheme
                ? "OnWallContact (a wall-climb — on touching a wall)"
                : jumps >= dodges
                    ? "OnJumpInAir (a double/air jump)"
                    : "OnDodge (a dash/dodge move)";
            steer = $"This play is MOVEMENT → root trigger {moveTrigger}; the effect is an Impulse (Up for a jump/climb, Forward for a dash). Match the trigger to the play/theme — a jump-heavy play is OnJumpInAir, a wall/climb theme is OnWallContact, a dodge is OnDodge. No offense.";
        }
        else if (attacks > 0)
            steer = "This play is OFFENSIVE → root trigger OnCast; build a Sequence: one Emit (its delivery is the shape — Projectile/Beam single-target, Burst/Nova area), then shape the attack with any of Damage (extra hit), Dot (a burn over time), Spread (hits extra targets — chain/pierce), Homing (the shot seeks), Control (Knockback/Slow/Stun). Mix these to make each attack distinct; don't just Emit+Damage every time.";
        else
            steer = "Defensive/ambient → root trigger Continuous; effect is a Ward.";

        return
$@"Compose a unique combat skill for a discovery as a deterministic EFFECT GRAPH.

Theme: {theme}
How the player fought: {play}
{fusionSteer}
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
