# ADR 0013 — Hit resolution is a simulation, not a physics query

Status: Accepted
Date: 2026-07-13
Amends: ADR 0006 ("Unity owns spatial hit detection" — it does not, any more)

## Context

Every combat judgment in the game was a Unity physics call:

| what it decides | how it decided it |
|---|---|
| did the bolt hit you | `Physics.SphereCastAll` |
| did the sword connect | `Physics.OverlapSphere` |
| did the pistol shot land | `Physics.Raycast` |
| who is inside the blast | `Physics.OverlapSphere` |
| did the spell bolt hit | `Physics.Linecast` |

So the engine decided **who got hit** — a game FACT, and in an MMOFPS the most contested fact
there is. Three things follow, and all three have already bitten us:

1. **It cannot be tested.** The runtime harness fuzzes graph execution headlessly, but it could
   never fuzz a hit, because a hit needed a Unity scene. The projectile bug — bolts dying the
   instant they were fired — lived for weeks in a system with 375 green tests, because not one
   of them could see it. It was found by *playing*, which is the most expensive place to find a
   bug.
2. **It cannot move to the server.** The MMO's authority model requires the server to resolve
   hits and the client to predict them. Code that calls `UnityEngine.Physics` cannot be run by
   ASP.NET Core. "Everything must remain replaceable" (Rule 5) was already false here.
3. **It is not deterministic.** PhysX results depend on scene state, layer setup, collider
   registration order, and (with trigger-based detection) on frame timing. Two players with the
   same aim can get different answers.

We had been treating the trajectory as sacred (`Ballistics`, a fixed-step deterministic core) and
the *hit* — the part that actually decides who dies — as an engine detail. That is backwards.

## Decision

**The collision world is a simulation.** `GameSimulation.Physics` owns a `CollisionWorld` of
simple bodies (sphere / capsule / box) and answers the only three questions combat asks:

- `SweepSphere(from, to, radius)` — projectiles and hitscan (a ray is a sweep of radius 0).
- `OverlapSphere(centre, radius)` — sword arcs and blast radii.
- Bodies carry an **actor id**; static level geometry is actor 0.

Unity's job shrinks to what a shell should do: it **describes** the world to the simulation (each
collider registers its equivalent body, actors update theirs as they move) and it **renders** the
answer. It no longer *reaches a verdict*. `UnityEngine.Physics` keeps only what is enactment, not
judgment: `CharacterController` movement.

The same package is already referenced by the API, so the day the server resolves hits, it runs
*this* code — unchanged.

## Consequences

- Hits become **fuzzable**: fire ten thousand bolts from inside walls, at absurd step sizes,
  through thin geometry, and assert nobody dies who shouldn't. The bug class that produced the
  "projectile vanishes on spawn" report is now a unit test.
- Boxes are swept as an expanded-box ray test, which is slightly generous at the corners. This is
  the standard approximation and is invisible at the radii we use; it is written down here so
  nobody rediscovers it as a bug.
- The blockout world must **register** itself. Anything that forgets to is not merely invisible to
  bullets — it does not exist to the game. That is the correct failure mode: the sim's world is
  the real world, and Unity's is the picture of it.
- This is not networking (still out of scope). It is the hook that makes networking possible —
  which is exactly what the vertical slice is supposed to leave behind.
