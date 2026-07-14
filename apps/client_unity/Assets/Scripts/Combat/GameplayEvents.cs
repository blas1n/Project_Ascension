using System;
using UnityEngine;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Combat
{
    /// <summary>
    /// The single stream of gameplay facts — things that actually happened, not
    /// inputs or intents. Each fact is raised by the system that owns the action at
    /// the moment it executes (past grounded/cooldown gates, on death, on pickup).
    /// Facts are domain-level and carry no consumer semantics: a fact is produced
    /// once and any number of observers (discovery relay, contracts, future
    /// achievements/analytics) subscribe. New observable thing = a new fact here.
    /// </summary>
    public static class GameplayEvents
    {
        // Player execution facts.
        public static event Action Jumped;
        public static event Action<bool> Attacked; // isMelee
        /// <summary>Something the player DID, with what, when, and what was true of them at the time
        /// (ADR 0009). The discovery grammar composes every composite behaviour from this one stream —
        /// there is no longer an event per idea.</summary>
        public static event Action<GameSimulation.Discovery.Act> ActPerformed;

        /// <summary>A weapon of this context ("arcane", "firearm", "melee", "bow") was just used.
        /// Carries WHAT was used, not merely that something was — so a fusion of the two hands can be
        /// observed instead of inferred from the loadout (ADR 0008).</summary>
        public static event Action<string> WeaponUsed;

        /// <summary>A weapon of this context just BEGAN a reload (dry-fire auto-reload or the Reload
        /// key). Feeds ActRecorder like any other verb — reload is an act, not a special case, so
        /// weaving something into a reload can compose into a discovery (ADR 0009).</summary>
        public static event Action<string> Reloaded;

        // Raw attack-button inputs (left/right click) — feed the command combo
        // recognizer, which needs the button press, not the weapon-fire execution.
        public static event Action LeftClicked;
        public static event Action RightClicked;

        // A discovered weapon asks the skill executor to run its skill (routing event;
        // the weapon lives in the Equipment assembly, the executor in Game).
        public static event Action<Skill> SkillCastRequested;

        // The player died (and will respawn) — the delegation tutorial uses this as its
        // teachable moment.
        public static event Action PlayerDied;

        /// <summary>The player explicitly committed a two-hand loadout at the Equipment Station —
        /// not merely opened the panel to look. The first hour's "첫 장비 선택" (stage 3) teachable
        /// moment; TutorialRunner turns this into TutorialSignal.EquipmentChosen.</summary>
        public static event Action EquipmentChosen;

        // A discovery finished composing on the server and its skill loaded — carries the
        // AI-composed NAME and how it manifests (weapon/command/passive). The single source of a
        // discovery's identity: the client no longer names discoveries locally (the server is
        // authoritative — ADR 0002/0004). The manifestation lets the toast (DiscoveryNotification)
        // say WHERE to claim it — a discovery enters the inventory/journal only, never the player's
        // hands automatically (see SkillCaster.OnSkillReady).
        public static event Action<string, ManifestationKind> SkillDiscovered;

        // World facts (argument is the subject GameObject).
        public static event Action<GameObject> MonsterKilled;
        public static event Action<GameObject> SampleCollected;
        public static event Action<GameObject> MarkerSurveyed;

        /// <summary>A monster's telegraphed strike whiffed because the target left AttackRange during
        /// the wind-up (ADR 0012 — evasion is movement: the tell is beaten by reading it and stepping
        /// out of range, not by a dodge button). Raised by the monster shell (MonsterBase) off
        /// MonsterAi's own Winding→Chase transition.</summary>
        public static event Action AttackEvaded;

        public static void RaiseJumped() => Jumped?.Invoke();
        public static void RaiseAttacked(bool isMelee) => Attacked?.Invoke(isMelee);
        public static void RaiseAttackEvaded() => AttackEvaded?.Invoke();
        public static void RaiseWeaponUsed(string contextTag) => WeaponUsed?.Invoke(contextTag);
        public static void RaiseReloaded(string contextTag) => Reloaded?.Invoke(contextTag);
        public static void RaiseActPerformed(GameSimulation.Discovery.Act act) => ActPerformed?.Invoke(act);

        public static void RaiseLeftClicked() => LeftClicked?.Invoke();
        public static void RaiseRightClicked() => RightClicked?.Invoke();

        public static void RaiseSkillCastRequested(Skill skill) => SkillCastRequested?.Invoke(skill);

        public static void RaisePlayerDied() => PlayerDied?.Invoke();
        public static void RaiseEquipmentChosen() => EquipmentChosen?.Invoke();

        public static void RaiseSkillDiscovered(string name, ManifestationKind manifestation) => SkillDiscovered?.Invoke(name, manifestation);

        public static void RaiseMonsterKilled(GameObject monster) => MonsterKilled?.Invoke(monster);
        public static void RaiseSampleCollected(GameObject sample) => SampleCollected?.Invoke(sample);
        public static void RaiseMarkerSurveyed(GameObject marker) => MarkerSurveyed?.Invoke(marker);
    }
}
