# ADR 0016 — Equip Before Train

Status: Accepted
Date: 2026-07-30
Supersedes: `docs/03-gameplay/first-hour-experience.md`'s 2단계/3단계 ordering (훈련장 before 첫 장비 선택).

## Context

Playtest (project owner): "훈련장이라고 안 느껴졌는데? ... 보통 뭘 때리라는 튜토 전에 무기를 선택해야 하고,
그 후에 입구에서 해야하지 않을까." (It didn't read as a training ground. Normally you'd pick a weapon
before being told to hit something, and only then do it at the entrance.)

The authored first hour (`TutorialDirector`'s `TutorialStep` sequence) sent the player to the training
ground BEFORE the equipment station: CreateCharacter → Training → ChooseEquipment → FirstDiscovery →
... The player was told to move/jump/evade/attack while still unarmed (or holding whatever placeholder
the shell defaults to), then only afterward chose the two weapons the training step was supposed to be
teaching.

This is backwards for two reasons:

- **You cannot meaningfully teach "attack" with a weapon the player never chose.** The training step's
  own requirement is `Moved | Jumped | Evaded | Attacked` — "Attacked" with what? A sword swings
  differently from a bow drawing differently from a pistol firing. Teaching the verb before the tool
  exists teaches the wrong tool, or no tool at all.
- **Picking your two hands is the first real expression of agency**, and the doc's own thesis
  (`첫 장비 선택`: "선택은 자유롭고 정답은 없다") is a stronger opening beat than being marched to a yard
  with nothing in your hands. Agency-first, then exercise the choice — not the other way around.

## Decision

**Equipment precedes training.** The `TutorialStep` order becomes:

```
CreateCharacter → ChooseEquipment → Training → FirstDiscovery → AcceptSurveyContract → ...
```

- `TutorialDirector`'s `TutorialStep` enum and `Requirement` map are reordered accordingly.
  `TutorialSignal` (the flags) is unchanged — flag values don't encode order, only the step sequence
  does.
- `TutorialGuideScript` leads with the equipment station ("Arm yourself. Two hands, two choices."),
  then sends the player to the yard with a line that acknowledges the loadout now exists ("Now put
  them to use — move, jump, and take a swing at that thing.").
- `docs/03-gameplay/first-hour-experience.md`'s stage 2/3 content is swapped: 2단계 = 첫 장비 선택,
  3단계 = 훈련장, with a note pointing at this ADR.
- Starter weapons (Sword/Pistol/Bow/Arcane Catalyst) must already be OWNED and selectable at the
  equipment station for a freshly created character — the station cannot depend on anything the
  (now-later) training step would have granted, because nothing after CreateCharacter granted them
  before either. This was already true; the reorder just makes it load-bearing from the first step
  onward instead of the second.

## Consequences

- A fresh player's very first physical destination is the armoury, not the yard. `TutorialGuideStations`
  must resolve `EquipmentStation` correctly the moment `ChooseEquipment` becomes the active step (right
  after naming), not merely by the time `Training` was reached under the old order — it already does,
  since `CityBlockout.EquipmentInteractable` is built at scene `Awake`, before any tutorial step runs.
- `TutorialDirector`'s and `TutorialGuideScript`'s test suites are reordered to assert the new sequence;
  no new signal or station was introduced, so no other system (contracts, discovery, death) is affected.
- This is a pure sequencing change. It does not touch combat, equipment ownership, or discovery rules.
