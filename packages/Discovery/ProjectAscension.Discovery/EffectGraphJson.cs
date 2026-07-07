using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace ProjectAscension.SkillForge;

/// <summary>
/// Serializes/parses an effect graph to/from JSON — the wire form the AI composes (ADR 0007
/// Phase 3) and the client is sent. Shape:
/// <code>{ "trigger":"OnCast", "effect": &lt;node&gt; }</code>
/// where a node is one of
/// <code>
/// {"kind":"Emit","delivery":"Projectile","tier":1}
/// {"kind":"Impulse","direction":"Up","tier":1}
/// {"kind":"Damage","tier":2}
/// {"kind":"Control","effect":"Stun","tier":1}
/// {"kind":"Ward","effect":"Shield","tier":1}
/// {"kind":"Sequence","steps":[ &lt;node&gt;, ... ]}
/// </code>
/// Parsing is total: any malformed / unknown-token input returns null (the pipeline then
/// retries/defers — no fallback, ADR 0002).
/// </summary>
public static class EffectGraphJson
{
    public static string Serialize(EffectNode root)
    {
        var sb = new StringBuilder();
        if (root is Trigger t)
        {
            sb.Append("{\"trigger\":\"").Append(t.Kind).Append("\",\"effect\":");
            WriteNode(sb, t.Child);
            sb.Append('}');
        }
        else
        {
            WriteNode(sb, root); // non-trigger root (invalid, but serialize for diagnostics)
        }
        return sb.ToString();
    }

    private static void WriteNode(StringBuilder sb, EffectNode node)
    {
        switch (node)
        {
            case Sequence s:
                sb.Append("{\"kind\":\"Sequence\",\"steps\":[");
                for (int i = 0; i < s.Steps.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    WriteNode(sb, s.Steps[i]);
                }
                sb.Append("]}");
                break;
            case Emit e:
                sb.Append("{\"kind\":\"Emit\",\"delivery\":\"").Append(e.Delivery).Append("\",\"tier\":").Append(e.Tier).Append('}');
                break;
            case Impulse i:
                sb.Append("{\"kind\":\"Impulse\",\"direction\":\"").Append(i.Direction).Append("\",\"tier\":").Append(i.Tier).Append('}');
                break;
            case Damage d:
                sb.Append("{\"kind\":\"Damage\",\"tier\":").Append(d.Tier).Append('}');
                break;
            case Control c:
                sb.Append("{\"kind\":\"Control\",\"effect\":\"").Append(c.Effect).Append("\",\"tier\":").Append(c.Tier).Append('}');
                break;
            case Ward w:
                sb.Append("{\"kind\":\"Ward\",\"effect\":\"").Append(w.Effect).Append("\",\"tier\":").Append(w.Tier).Append('}');
                break;
            case Dot dot:
                sb.Append("{\"kind\":\"Dot\",\"tier\":").Append(dot.Tier).Append(",\"duration\":").Append(dot.Duration).Append('}');
                break;
            case Spread sp:
                sb.Append("{\"kind\":\"Spread\",\"tier\":").Append(sp.Tier).Append('}');
                break;
            case Homing h:
                sb.Append("{\"kind\":\"Homing\",\"tier\":").Append(h.Tier).Append('}');
                break;
            default:
                sb.Append("null");
                break;
        }
    }

    public static EffectNode? Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            if (!root.TryGetProperty("trigger", out var trig) || trig.ValueKind != JsonValueKind.String) return null;
            if (!TryEnum<TriggerKind>(trig.GetString(), out var triggerKind)) return null;
            if (!root.TryGetProperty("effect", out var effect)) return null;
            var child = ParseNode(effect);
            return child is null ? null : new Trigger(triggerKind, child);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static EffectNode? ParseNode(JsonElement e)
    {
        if (e.ValueKind != JsonValueKind.Object) return null;
        if (!e.TryGetProperty("kind", out var kindEl) || kindEl.ValueKind != JsonValueKind.String) return null;
        switch (kindEl.GetString())
        {
            case "Sequence":
                if (!e.TryGetProperty("steps", out var steps) || steps.ValueKind != JsonValueKind.Array) return null;
                var list = new List<EffectNode>();
                foreach (var s in steps.EnumerateArray())
                {
                    var n = ParseNode(s);
                    if (n is null) return null;
                    list.Add(n);
                }
                return new Sequence(list);
            case "Emit":
                return TryEnum<EmitDelivery>(Str(e, "delivery"), out var del) && TryTier(e, out var et)
                    ? new Emit(del, et) : null;
            case "Impulse":
                return TryEnum<ImpulseDirection>(Str(e, "direction"), out var dir) && TryTier(e, out var it)
                    ? new Impulse(dir, it) : null;
            case "Damage":
                return TryTier(e, out var dt) ? new Damage(dt) : null;
            case "Control":
                return TryEnum<ControlEffect>(Str(e, "effect"), out var ce) && TryTier(e, out var ct)
                    ? new Control(ce, ct) : null;
            case "Ward":
                return TryEnum<WardEffect>(Str(e, "effect"), out var we) && TryTier(e, out var wt)
                    ? new Ward(we, wt) : null;
            case "Dot":
                return TryTier(e, out var dott) ? new Dot(dott, TryInt(e, "duration", out var dur) ? dur : 0) : null;
            case "Spread":
                return TryTier(e, out var spt) ? new Spread(spt) : null;
            case "Homing":
                return TryTier(e, out var ht) ? new Homing(ht) : null;
            default:
                return null;
        }
    }

    private static string? Str(JsonElement e, string prop)
        => e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static bool TryTier(JsonElement e, out int tier) => TryInt(e, "tier", out tier);

    private static bool TryInt(JsonElement e, string prop, out int value)
    {
        value = 0;
        return e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out value);
    }

    private static bool TryEnum<T>(string? s, out T value) where T : struct
        => Enum.TryParse(s, ignoreCase: true, out value) && Enum.IsDefined(typeof(T), value);
}
