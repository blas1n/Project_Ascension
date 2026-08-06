# ADR 0017 — First Discovery Onboarding

Status: Accepted
Date: 2026-08-06
Amends: `docs/04-technical/adr/0010-discovery-economy.md` (not overturned — see Relationship to ADR 0010 below).

## Context

Reproduced against the live server: a new player fights in the training yard — ordinary repeated
basic attacks and jumps — and no discovery ever fires, so the tutorial's `FirstDiscovery` stage
never completes.

The measured numbers: that ordinary play scored ~82 after a single ~5s activity window
(`persistence=1`). It only crossed `FireThreshold` (200) at `persistence=5` — roughly 25s of
**unbroken** activity. `DiscoveryReporter` resets `persistence` to 0 whenever a 5s flush window sees
no activity, so intermittent training-yard play (the realistic shape of a first-timer poking at a
dummy, pausing, trying something else) never accumulates that far. The player also asked "is magic
un-discoverable?" — it is not; arcane play scores through the exact same formula as melee or
firearm play (see `SkillCompositionServiceTests` and `TriggerEvaluatorTests`, both of which already
exercise `"arcane"` as a context factor and fire normally). The training yard's plain, low-variety
play simply never reached 200 for ANY loadout.

`FireThreshold` = 200 is deliberate (ADR 0010 §1-c/1-d): it is the price of the first rung of a
style's ladder, sized so that a single spell-cast-fused-into-a-mag-dump doesn't clear the bar. That
tuning is correct for the STEADY-STATE economy. It is wrong as an ONBOARDING gate — a first-timer
who has never made a discovery before doesn't yet know discovery responds to HOW they fight, and
25 unbroken seconds of the same plain swing is not a reasonable ask before they've been shown the
system works at all.

## Decision

**The actor's LIFETIME-FIRST discovery — no row in the `Discoveries` table for them yet, in ANY
style or region — gates on a new, low, DB-driven `FirstDiscoveryThreshold` instead of the normal
rung score. Every discovery after it is back on the full economy (`FireThreshold` = 200, exponential
rungs, ADR 0010 unchanged).**

### 1. Detection: no client flag

The server already knows whether this is a player's first discovery — `IDiscoveryRepository
.GetByActorAsync(actorId)` returns zero rows. `SkillCompositionService.EvaluateAndTriggerAsync`
checks this directly; no client-asserted "is this my first discovery" flag is trusted or needed.

### 2. The gate, precisely

In the existing rung logic (`best` = the actor's highest claimed rung in this STYLE, `next` = the
rung being attempted):

```csharp
var isLifetimeFirst = (await _discoveries.GetByActorAsync(request.ActorId, ct)).Count == 0;
var requiredScore = isLifetimeFirst ? tuning.FirstDiscoveryThreshold : TriggerEvaluator.RungScore(next, tuning);
if (outcome.Score < requiredScore)
    return new EvaluateTriggerResponse(false, outcome.Score, null);
```

For a lifetime-first actor, `best` is always `null` and `next` is always `Rarity.Common` (they have
claimed nothing, anywhere) — so this only ever substitutes the threshold for the Common rung of
whatever style they happen to compose first. It does not touch `RungScore` for Uncommon/Rare/Epic/
Legendary, and it does not touch a SECOND style's first rung once the actor has one discovery
anywhere: `isLifetimeFirst` is a lifetime fact, not a per-style one, matching "the LIFETIME-FIRST
discovery" in the brief exactly (a player's second-ever discovery, even in a style they've never
touched, is back on the normal Common rung — that stays ADR 0010's breadth-pays-in-quantity rule).

### 3. The value: 70

`DiscoveryTuning.Default.FirstDiscoveryThreshold = 70` (seeded identically in
`DiscoveryTuningSettingsConfiguration`).

Arithmetic: the repro session scored ≈82 at `persistence=1` — one ordinary ~5s window of attacks
and jumps. 70 sits below that, so the **exact same repro session now fires on its first or second
activity window**, without needing sustained unbroken play. It is not free: a bare handful of idle
jumps alone (`Jump` weight 1, no combination synergy at a single distinct behaviour, `distinct − 1 =
0`) would need on the order of 65+ presses in one window to clear it solo, and `persistence` only
grows on windows with real activity — so the threshold still asks for an actual, if small,
COMPOSITION of behaviour (an attack plus a jump, or persistence across two windows), not a single
keystroke.

### 4. Everything else about the first discovery stays real

Only the SCORE gate for the lifetime-first discovery is lowered. Unaffected:

- **It is composed from real behaviour**, not a scripted gift (ADR 0002) — the same
  `CreateDiscoveryAsync` → `ComposePendingAsync` path runs; the AI composes name/description/graph
  from whatever the player actually did.
- **Dedup** — the idempotency-key / claim-key logic (`{styleKey}:{rarity}`) is untouched; a repeated
  hit at the lowered threshold still returns the existing discovery rather than minting a duplicate.
- **The style ladder** — the claim is still keyed by play style (weapons that took part + delivery +
  region), and still costs a real rung once the actor has their first discovery.
- **Rarity climbs one rung at a time** and **a style never touched before starts at Common** (ADR
  0010 §1-c/1-d) — the lifetime-first discovery is always Common by construction (see §2), so this
  doesn't create a shortcut to a higher rarity.

### 5. Storage: `DiscoveryTuningSettings` → DB tuning row

`FirstDiscoveryThreshold` is a balance number, so it follows the project's existing pattern for
`FireThreshold` et al.: `DiscoveryTuningSettings` (Domain entity, singleton row) →
`DiscoveryTuningSettingsConfiguration` (EF seed) → migration `FirstDiscoveryOnboarding` (adds the
column, backfills the existing singleton row to 70) → `DiscoveryTuningProvider` maps it into the
pure `DiscoveryTuning` record the rule engine reads. It is NOT hardcoded, and NOT added to
`DiscoveryTuningResponse` — nothing on the client needs to see it; the server alone decides whether
an evaluate call is this actor's first.

### 6. The tutorial guide actively leads toward a discovery

Lowering the threshold makes discovery reachable from PLAIN play, but `TutorialGuideScript`'s
`FirstDiscovery` line was passive ("Fight it your own way. See what happens.") — not enough for a
first-timer who doesn't know discovery reads HOW they fight. The line now nudges toward a concrete,
easy COMPOSITION in the doc's own spirit (`docs/03-gameplay/first-hour-experience.md`'s 첫 발견
examples: 반복 점프 / 공중 공격 / 회피 직후 공격) — "strike while airborne" or "weave two things
together" — without scripting a specific skill or forcing a specific outcome (ADR 0002: the player
still has to act; the guide only makes the fertile behaviour legible). `TutorialGuideStation` for
this step stays `None` — discovery is behavioural, not a place to walk to.

## Relationship to ADR 0010

ADR 0010 is **amended, not overturned**. Its economy (exponential rung spacing, one-rung-at-a-time
climbing, a never-touched style starting at Common, depth adding lineage not score) governs every
discovery a player ever makes — including their first, once they have made ANY discovery before.
This ADR carves out exactly one exception: the very first discovery of a player's lifetime uses a
different, lower gate so the tutorial's core loop (City → Contract → Expedition → Combat →
**Discovery** → Return → Reward) is reachable on the first pass through Combat, instead of requiring
sustained grinding a first-timer has no reason to expect works. Scarcity for the second discovery
onward, in any style, is unchanged.

## Consequences

- A fresh player who fights normally in the training yard for even one active window will very
  likely cross 70 and get their first discovery — the tutorial's `FirstDiscovery` stage becomes
  completable through ordinary play, not a 25-second grind.
- No change to the steady-state economy: `FireThreshold` (200) and the exponential rung ladder are
  untouched for every discovery after the first.
- `SkillCompositionServiceTests` gains coverage: an actor with zero prior discoveries fires at
  `FirstDiscoveryThreshold`; an actor with one prior discovery does NOT fire at that same low score
  in a fresh style (must reach the full `FireThreshold`/Common rung); the lifetime-first discovery
  still dedups on repeat.
- Magic was never the problem: arcane-driven play scores through the identical formula as melee/
  firearm (same `TriggerEvaluator.Evaluate`, same factor table — `"arcane"` is a real, weighted
  Equipment factor). Existing tests already exercised this; this ADR's test pass adds an explicit,
  named assertion so the fact is easy to point at.
