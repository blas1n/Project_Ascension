# ADR 0015 — Cooldowns, not Focus

Status: Accepted
Date: 2026-07-14
Supersedes: the 집중력(Focus) resource in `docs/02-systems/combat-framework.md` (전투 자원), and the
"포커스 비용 → 그래프 Cost" migration row in ADR 0007's phase table.

## Context

Skills were gated by a Focus pool: a regenerating resource, spent per cast in proportion to the
skill's structural cost.

It never earned its place.

- **It gated nothing.** Focus regenerated on a timer, so the only thing it could do was make you wait —
  which is a cooldown, paid in a second currency, with a bar to watch. Two systems, one behaviour.
- **It flattened the skills.** With one shared pool, every skill competes for the same budget, so what
  you feel is your *pool*, not your *skill*. A discovery game whose whole promise is "your own skill,
  your own path" must make the skill itself the thing you feel — its own rhythm, its own wait.
- **It cost the player attention for nothing.** In an FPS you are already tracking health, a magazine,
  a reload and a monster's wind-up. A fifth bar that only ever says "wait" is noise.

## Decision

**Focus is deleted. Every skill has its own cooldown.**

- A skill's cooldown is **derived from what the skill IS** — its effect graph's structural power
  (ADR 0007/0010) × a DB-driven seconds-per-point, clamped to a floor and a ceiling. It is not
  authored per skill, because **skills are composed at runtime** and nobody is standing by to
  hand-tune each one. A bigger skill makes you wait longer, deterministically, forever.
- The numbers (per-point, floor, ceiling) are **DB tuning**, like every other balance number.
- The UI is the Overwatch shape: **the ability is the thing on screen**, one framed box per key, dark
  and counting while it recharges, bright when it is ready. You read your kit at a glance instead of
  doing arithmetic against a bar.

## Consequences

- Power is expressed as **time**, and the rule engine already knows a skill's structural cost — so
  the same budget that stops a discovery being too strong now also decides how long it makes you
  wait. One measure, two jobs, no new knob.
- Passives are unaffected (they are always on and have no cast).
- Weapons keep their own separate fire-rate cooldown (`WeaponFireRules`) and their magazines. A skill's
  cooldown and a weapon's rate of fire are different clocks and should stay that way.
- `combat-framework.md`'s 전투 자원 section is superseded by this ADR and should be rewritten when that
  document is next revised.
