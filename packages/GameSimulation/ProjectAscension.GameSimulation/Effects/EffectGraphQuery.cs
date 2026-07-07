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
