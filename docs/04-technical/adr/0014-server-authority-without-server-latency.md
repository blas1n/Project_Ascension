# ADR 0014 — Server authority without server latency

Status: Accepted
Date: 2026-07-13
Related: ADR 0013 (hit resolution is a simulation), ADR 0006 (client is a view/prediction)

## Context

"The server decides everything" sounds like "every shot waits 80 ms". It does not — but only if
we are precise about what authority means and where it is paid for.

**Authority is not a round-trip.** Server-authoritative means *the server's answer wins*, not
*we wait for the server before drawing anything*. The mistake is to pick one latency policy for
the whole game.

## Decision

Systems are classified by **latency sensitivity**, and each class gets a different policy.

### Class A — predicted (combat, movement): 0 ms perceived

The client runs the **same deterministic simulation code** the server runs (`GameSimulation` is one
package, referenced by both the Unity client and the API), enacts the result immediately, and the
server re-simulates and reconciles. Because the simulation is deterministic and the inputs are the
same, prediction and authority **agree** almost always; a misprediction is a rare correction, not a
constant fight.

Three properties make that possible, and all three are already in place:

1. **One simulation, two hosts.** `PlayerSimulation`, `CollisionWorld`, `ProjectileSim`,
   `MonsterAi`, `WeaponFireRules` — no `UnityEngine` anywhere in them (ADR 0013).
2. **Deterministic randomness with a REPRODUCIBLE seed.** Spread is sampled by a seeded PRNG from
   `(weapon seed, shot index)` — facts the server can recompute on its own. This is the correct form
   of "share the seed and pre-compute": the client does not *choose* a seed and tell the server
   (it would pick a flattering one) — the client and the server **independently derive the same
   seed from the same facts**.
3. **Rules take the clock as an argument.** Nothing reads `Time.time` to decide; time is passed in,
   so a re-simulation at a different wall-clock reaches the same answer.

What remains for the netcode phase (NOT now — MMO networking is out of the vertical slice):
input sequence numbers, server snapshots, rollback-and-replay of unacknowledged inputs, and lag
compensation (the server rewinds targets to what the shooter actually saw). The architecture is
shaped for it; none of it is built.

### Class B — round-trip (economy, contracts, discovery): 100 ms, and nobody notices

Turn-ins, purchases, licensing, escrow, rewards, reputation, discovery facts. **Gold has no reason
to appear within 16 ms.** These get a plain request → server computes from its own data → response
is the truth. No prediction.

### Never predicted

Rewards, drop rolls, whether a discovery occurred, and who discovered it first. Not because they are
slow — because **taking them back is unbearable**. A shot that un-hits is a hiccup; a legendary that
un-discovers is a betrayal. The cost of a wrong prediction, not its likelihood, is what decides.

## Consequences

- The client may compute combat immediately and **must not** compute economy at all.
- A predicted value that the server contradicts is corrected silently for combat, and cannot occur
  for economy because the client never guesses one.
- Every rule we move into `GameSimulation` (rather than a MonoBehaviour) is not architectural
  hygiene — it is the *only* reason prediction can ever be correct. A rule that lives in Unity is
  a rule the server cannot re-run, and therefore a rule the client cannot safely predict.
