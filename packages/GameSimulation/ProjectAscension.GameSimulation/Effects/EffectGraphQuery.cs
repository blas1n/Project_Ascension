namespace ProjectAscension.GameSimulation.Effects
{
    /// <summary>
    /// Small read helpers over an effect graph for the presentation layer (ADR 0007 Phase 4b) —
    /// what delivery SHAPE the skill emits (drives VFX) and whether it homes. The combat NUMBERS
    /// live in GraphSkillResolver; this is only for choosing visuals/behaviour.
    /// </summary>
    public static class EffectGraphQuery
    {
        /// <summary>The first Emit's delivery in the graph, or null if it emits nothing
        /// (e.g. a pure control/ward skill).</summary>
        public static EmitDelivery? FirstDelivery(EffectNode graph) => Find(graph);

        /// <summary>Delivery as the lowercase style string the client's DeliveryStyles keys on
        /// ("projectile"/"beam"/"burst"/"nova"), or "" when the graph emits nothing.</summary>
        public static string DeliveryStyle(EffectNode graph)
        {
            var d = FirstDelivery(graph);
            return d.HasValue ? d.Value.ToString().ToLowerInvariant() : string.Empty;
        }

        public static bool HasHoming(EffectNode graph) => Contains(graph, isHoming: true);

        /// <summary>The graph's "power points" — the tier-weighted size (Σ tier+1 over effect
        /// nodes, + DoT durations). The graph analogue of a primitive skill's Σ(magnitude+range+
        /// duration); drives focus cost and knowledge value so a bigger skill costs/sells more.</summary>
        public static int PowerPoints(EffectNode node)
        {
            switch (node)
            {
                case null: return 0;
                case Trigger t: return PowerPoints(t.Child);
                case Sequence s:
                    int sum = 0;
                    foreach (var step in s.Steps) sum += PowerPoints(step);
                    return sum;
                case Emit e: return e.Tier + 1;
                case Damage d: return d.Tier + 1;
                case Control c: return c.Tier + 1;
                case Ward w: return w.Tier + 1;
                case Impulse i: return i.Tier + 1;
                case Spread sp: return sp.Tier + 1;
                case Homing h: return h.Tier + 1;
                case Dot dot: return dot.Tier + 1 + dot.Duration;
                default: return 0;
            }
        }

        // VFX accent flags — which impact accents to play (mirrors SkillVfx's primitive switch).
        public static bool HasSpread(EffectNode graph) => Any(graph, n => n is Spread);
        public static bool HasKnockback(EffectNode graph) => Any(graph, n => n is Control c && c.Effect == ControlEffect.Knockback);
        public static bool HasLeech(EffectNode graph) => Any(graph, n => n is Ward w && w.Effect == WardEffect.Leech);
        public static bool HasDot(EffectNode graph) => Any(graph, n => n is Dot);

        /// <summary>The longest DoT duration in the graph (0 = no burn) — for a lingering VFX pool.</summary>
        public static int MaxDotDuration(EffectNode node)
        {
            switch (node)
            {
                case Dot d: return d.Duration;
                case Trigger t: return MaxDotDuration(t.Child);
                case Sequence s:
                    int max = 0;
                    foreach (var step in s.Steps) { int d2 = MaxDotDuration(step); if (d2 > max) max = d2; }
                    return max;
                default: return 0;
            }
        }

        private static bool Any(EffectNode node, System.Func<EffectNode, bool> pred)
        {
            if (node is null) return false;
            if (pred(node)) return true;
            switch (node)
            {
                case Trigger t: return Any(t.Child, pred);
                case Sequence s:
                    foreach (var step in s.Steps) if (Any(step, pred)) return true;
                    return false;
                default: return false;
            }
        }

        private static EmitDelivery? Find(EffectNode node)
        {
            switch (node)
            {
                case Emit e: return e.Delivery;
                case Trigger t: return Find(t.Child);
                case Sequence s:
                    foreach (var step in s.Steps)
                    {
                        var found = Find(step);
                        if (found.HasValue) return found;
                    }
                    return null;
                default: return null;
            }
        }

        private static bool Contains(EffectNode node, bool isHoming)
        {
            switch (node)
            {
                case Homing: return isHoming;
                case Trigger t: return Contains(t.Child, isHoming);
                case Sequence s:
                    foreach (var step in s.Steps)
                        if (Contains(step, isHoming)) return true;
                    return false;
                default: return false;
            }
        }
    }
}
