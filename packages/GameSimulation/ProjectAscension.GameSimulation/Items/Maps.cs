namespace ProjectAscension.GameSimulation.Items
{
    /// <summary>
    /// What a map is FOR. The doc insists the map is a possession rather than a UI panel — "지도는
    /// 자산이다" — but a possession only means something if holding it changes what you can do. So the
    /// way into the deep frontier is unmapped ground: you cannot find the pass without the chart.
    ///
    /// That makes the first hour causal instead of merely sequential — the survey (stage 6) is what
    /// buys the deep contract (stage 7) — and it gives the later stakes teeth: a map you can lose is a
    /// map whose loss closes a road.
    /// </summary>
    public static class Maps
    {
        /// <summary>The chart the outskirts survey pays out (seeded as an ItemDefinition).</summary>
        public const string FrontierMapKey = "frontier_map";

        /// <summary>Whether the player can find the way into the deep frontier. Without the chart the
        /// pass simply isn't findable — this is a possession check, not a permission flag.</summary>
        public static bool CanEnterDeepFrontier(Inventory inventory)
            => inventory != null && inventory.Has(FrontierMapKey);
    }
}
