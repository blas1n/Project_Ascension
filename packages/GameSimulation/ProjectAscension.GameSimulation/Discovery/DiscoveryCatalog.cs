using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Discovery
{
    /// <summary>
    /// The MVP discovery catalog. Context tags come from equipped gear
    /// ("melee", "firearm", "bow", "arcane"). Note flame_bullet / flame_lance /
    /// thermal_barrier share the "arcane" context but differ by behavior — the same
    /// combination yields a different discovery based on how the player fights.
    /// Titles/descriptions are authored here (AI flavor is a later hook).
    /// </summary>
    public static class DiscoveryCatalog
    {
        private static readonly string[] NoContext = new string[0];
        private static readonly string[] Arcane = { "arcane" };
        private static readonly string[] Firearm = { "firearm" };
        private static readonly string[] Melee = { "melee" };

        public static readonly IReadOnlyList<DiscoveryCandidate> All = new[]
        {
            // Behavior discoveries (no equipment context).
            new DiscoveryCandidate("double_jump", "Double Jump", "Repeated leaping revealed a second jump.", BehaviorKind.Jump, NoContext, 30),
            new DiscoveryCandidate("evasive_roll", "Evasive Roll", "Constant dodging honed an evasive roll.", BehaviorKind.Dodge, NoContext, 30),
            new DiscoveryCandidate("dodge_slash", "Dodge Slash", "Striking out of a dodge became a counter.", BehaviorKind.DodgeAttack, NoContext, 15),

            // Same context (arcane), different behavior -> different discovery.
            new DiscoveryCandidate("flame_bullet", "Flame Bullet", "Arcane fire focused into a precise bolt.", BehaviorKind.RangedAttack, Arcane, 20),
            new DiscoveryCandidate("flame_lance", "Flame Lance", "Arcane fire shaped into a melee lance.", BehaviorKind.MeleeAttack, Arcane, 20),
            new DiscoveryCandidate("thermal_barrier", "Thermal Barrier", "Arcane heat turned into a defensive ward.", BehaviorKind.Dodge, Arcane, 20),

            // Equipment-specialized.
            new DiscoveryCandidate("rapid_fire", "Rapid Fire", "Relentless shooting unlocked rapid fire.", BehaviorKind.RangedAttack, Firearm, 30),
            new DiscoveryCandidate("blade_dance", "Blade Dance", "Endless swings flowed into a blade dance.", BehaviorKind.MeleeAttack, Melee, 30),
            new DiscoveryCandidate("riposte", "Riposte", "Countering out of a dodge became a riposte.", BehaviorKind.DodgeAttack, Melee, 15),

            // Discovery graph (a discovery becomes the seed for the next).
            new DiscoveryCandidate("high_jump", "High Jump", "Mastery of the double jump reached higher.", BehaviorKind.Jump, NoContext, 40, "double_jump"),
            new DiscoveryCandidate("storm_step", "Storm Step", "The evasive roll quickened into a storm step.", BehaviorKind.Dodge, NoContext, 40, "evasive_roll"),
        };
    }
}
