# ADR 0012 — Evasion is movement, not a button

Status: Accepted
Date: 2026-07-13
Supersedes: the dodge mechanic in Vertical Slice Phase 1; the `Dodge` verb / `Dodging`
quality of ADR 0009; the `OnDodge` trigger of ADR 0007; the dodge signals of ADR 0003.

## Context

Phase 1 said "movement, jump, dodge, camera", so we built a dodge: a button that fires a
burst impulse and grants invulnerability frames. Monster attacks then grew a telegraph
(wind-up) so the i-frames had something to beat.

But this is a **first-person shooter**, and the i-frame roll is not an FPS verb — it is a
Soulslike/ARPG verb. In an FPS you evade with **distance, cover and strafing**: you read
the tell and you are not standing there any more. Destiny and Overwatch have no universal
dodge button; what they have is *character-specific mobility* (blink, dash, thruster) —
an **ability**, not a birthright.

Two things made the button redundant rather than merely unfashionable:

1. **The spatial dodge already exists and already works.** `MonsterAi` cancels a strike
   when the target leaves `AttackRange` during the wind-up — the telegraph is already paid
   off by walking backwards. The i-frame button was a second, parallel answer to a question
   the movement system had already answered.
2. **It made position not matter.** A button that says "this hit does not count" competes
   with the entire reason to look at where you are standing. The telegraph should be read
   with your feet.

## Decision

**There is no dodge button.** Evasion is movement, cover, and reading the tell.

- The `Dodge` input action, the dodge impulse, and the invulnerability window are removed
  from the player simulation. Blocking (hold the shield hand) remains — it is a *stance*,
  not an i-frame, and it costs mobility, so it does not have this problem.
- Because the act no longer happens, its **observation is removed too**: the `Dodge` verb,
  the `Dodging` quality, the `OnDodge` graph trigger and the `Dodge`/`DodgeAttack` behavior
  weights all go. A signal nobody can emit is a lie in the tuning table (see the dead
  `DodgeAttack` weight this project already shipped once).
- **Mobility becomes discoverable.** A dash/blink is not taken away — it is *earned*. The
  effect graph already has `Impulse`, and a Command manifestation carrying one **is** a
  dash, bound to the weapons that made it (ADR 0011) and sitting on a hotkey. That is the
  Overwatch/Destiny shape, and it is also this project's shape: your mobility is *your*
  discovery, not a key everyone was issued at birth.

## Consequences

- Melee monsters are beaten by spacing, ranged monsters by cover and strafing. The
  telegraph keeps its meaning — it is now the *only* meaning it has.
- The player's baseline kit is smaller: move, jump, attack, block, interact. Everything
  else is discovered. This is the correct direction for a discovery game.
- The freed keys let the command hotbar take the conventional slots (see the control map).
- Phase 1 of the Vertical Slice is amended: "movement, jump, dodge, camera" →
  "movement, jump, camera".
