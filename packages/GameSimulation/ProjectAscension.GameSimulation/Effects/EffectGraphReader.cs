using System;
using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Effects
{
    /// <summary>
    /// Parses the effect-graph JSON the API serves (ADR 0007) into the client
    /// <see cref="EffectNode"/> model — the mirror of the server's EffectGraphJson.Serialize. The
    /// shape is <c>{"trigger":"OnCast","effect":&lt;node&gt;}</c>. Total: malformed / unknown-token
    /// input returns null and the skill is treated as graphless (no movement capability from it).
    /// </summary>
    public static class EffectGraphReader
    {
        public static EffectNode Parse(string json)
        {
            if (!(MiniJson.Parse(json) is Dictionary<string, object> root)) return null;
            if (!(Get(root, "trigger") is string trig) || !TryEnum<TriggerKind>(trig, out var kind)) return null;
            if (!(Get(root, "effect") is Dictionary<string, object> effect)) return null;
            var child = ParseNode(effect);
            return child is null ? null : new Trigger(kind, child);
        }

        private static EffectNode ParseNode(Dictionary<string, object> e)
        {
            if (!(Get(e, "kind") is string kind)) return null;
            switch (kind)
            {
                case "Sequence":
                    if (!(Get(e, "steps") is List<object> steps)) return null;
                    var list = new List<EffectNode>(steps.Count);
                    foreach (var s in steps)
                    {
                        if (!(s is Dictionary<string, object> so)) return null;
                        var n = ParseNode(so);
                        if (n is null) return null;
                        list.Add(n);
                    }
                    return new Sequence(list);
                case "Emit":
                    return TryEnum<EmitDelivery>(Get(e, "delivery") as string, out var del) && TryTier(e, out var et)
                        ? new Emit(del, et) : null;
                case "Impulse":
                    return TryEnum<ImpulseDirection>(Get(e, "direction") as string, out var dir) && TryTier(e, out var it)
                        ? new Impulse(dir, it) : null;
                case "Damage":
                    return TryTier(e, out var dt) ? new Damage(dt) : null;
                case "Control":
                    return TryEnum<ControlEffect>(Get(e, "effect") as string, out var ce) && TryTier(e, out var ct)
                        ? new Control(ce, ct) : null;
                case "Ward":
                    return TryEnum<WardEffect>(Get(e, "effect") as string, out var we) && TryTier(e, out var wt)
                        ? new Ward(we, wt) : null;
                default:
                    return null;
            }
        }

        private static object Get(Dictionary<string, object> d, string key)
            => d.TryGetValue(key, out var v) ? v : null;

        private static bool TryTier(Dictionary<string, object> e, out int tier)
        {
            tier = 0;
            if (Get(e, "tier") is double d) { tier = (int)d; return true; }
            return false;
        }

        private static bool TryEnum<T>(string s, out T value) where T : struct
            => Enum.TryParse(s, ignoreCase: true, out value) && Enum.IsDefined(typeof(T), value);
    }
}
