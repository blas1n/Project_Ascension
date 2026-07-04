namespace ProjectAscension.SkillForge;

/// <summary>The discrete button inputs a command's invocation combo is built from —
/// the player presses this sequence to invoke the command. Mirrored on the client
/// (names match the response strings).</summary>
public enum InputToken
{
    Jump,
    Dodge,
    LeftClick,
    RightClick,
}

/// <summary>
/// Assigns a command its invocation combo — the button sequence the player performs
/// to invoke it. The rule engine derives it from the behaviors that discovered the
/// skill, mapped to buttons, so the combo reads naturally (jump → "jump, jump";
/// dodge-then-attack → "dodge, left-click"). A single behavior is repeated — double
/// jump is invoked by jumping again, the conventional feel. When no behaviors are
/// recorded (e.g. a manually triggered discovery) it falls back to a deterministic
/// hash of the seed. Deterministic and server-authoritative (not the AI's call).
/// </summary>
public static class ComboAssigner
{
    private static readonly InputToken[] Vocabulary =
        { InputToken.Jump, InputToken.Dodge, InputToken.LeftClick, InputToken.RightClick };

    public const int MinLength = 2;
    public const int MaxLength = 4;

    public static IReadOnlyList<InputToken> Assign(IEnumerable<string>? behaviors, string seed)
    {
        var combo = new List<InputToken>();
        if (behaviors is not null)
        {
            foreach (var behavior in behaviors)
            {
                if (!TryMap(behavior, out var token)) continue;          // skip derived/unknown (e.g. DodgeAttack)
                if (combo.Count > 0 && combo[^1] == token) continue;     // no immediate repeat
                combo.Add(token);
            }
        }

        if (combo.Count == 0) return FromSeed(seed);   // no behaviors → deterministic fallback
        if (combo.Count == 1) combo.Add(combo[0]);     // single behavior → repeat (double jump = jump, jump)
        return combo;
    }

    private static bool TryMap(string behavior, out InputToken token)
    {
        token = default;
        if (string.IsNullOrEmpty(behavior)) return false;
        if (behavior.Equals("Jump", StringComparison.OrdinalIgnoreCase)) { token = InputToken.Jump; return true; }
        if (behavior.Equals("Dodge", StringComparison.OrdinalIgnoreCase)) { token = InputToken.Dodge; return true; }
        if (behavior.Equals("MeleeAttack", StringComparison.OrdinalIgnoreCase)) { token = InputToken.LeftClick; return true; }
        if (behavior.Equals("RangedAttack", StringComparison.OrdinalIgnoreCase)) { token = InputToken.RightClick; return true; }
        return false;
    }

    private static IReadOnlyList<InputToken> FromSeed(string seed)
    {
        uint hash = Fnv(seed ?? string.Empty);
        int length = MinLength + (int)(hash % (uint)(MaxLength - MinLength + 1));

        var combo = new List<InputToken>(length);
        for (int i = 0; i < length; i++)
        {
            hash = (hash ^ (uint)(i + 1)) * 16777619u;
            int index = (int)(hash % (uint)Vocabulary.Length);
            var token = Vocabulary[index];
            if (combo.Count > 0 && token == combo[^1])
                token = Vocabulary[(index + 1) % Vocabulary.Length];
            combo.Add(token);
        }
        return combo;
    }

    private static uint Fnv(string s)
    {
        uint hash = 2166136261u;
        foreach (char c in s)
        {
            hash ^= c;
            hash *= 16777619u;
        }
        return hash;
    }

    // ----- Prefix-free guarantee -------------------------------------------------------------
    // No command's combo may be a PREFIX of another's (per actor). Then the client can fire the
    // instant a combo completes — no wait-and-see disambiguation (which added input latency).
    // This is how fighting games avoid "a short move shadows the longer string": distinct,
    // non-overlapping inputs.

    /// <summary>Return <paramref name="candidate"/> if it collides with no
    /// <paramref name="existing"/> combo (neither is a prefix of the other); otherwise the
    /// shortest deterministic alternative that is prefix-free (short combos preferred).</summary>
    public static IReadOnlyList<InputToken> EnsurePrefixFree(
        IReadOnlyList<InputToken> candidate,
        IEnumerable<IReadOnlyList<InputToken>> existing,
        string seed)
    {
        var taken = new List<IReadOnlyList<InputToken>>();
        if (existing is not null)
            foreach (var e in existing)
                if (e is { Count: > 0 }) taken.Add(e);

        if (candidate is { Count: > 0 } && IsPrefixFree(candidate, taken)) return candidate;

        foreach (var combo in AllCombos()
            .OrderBy(c => c.Count)               // prefer short combos
            .ThenBy(c => Fnv(seed + Key(c))))    // deterministic, seed-varied tie-break
        {
            if (IsPrefixFree(combo, taken)) return combo;
        }
        return candidate; // vocabulary saturated — unreachable for the slice's few commands
    }

    private static bool IsPrefixFree(IReadOnlyList<InputToken> combo, List<IReadOnlyList<InputToken>> taken)
    {
        foreach (var e in taken)
            if (IsPrefix(combo, e) || IsPrefix(e, combo)) return false; // either direction collides
        return true;
    }

    // Is <paramref name="prefix"/> a prefix of <paramref name="seq"/>?
    private static bool IsPrefix(IReadOnlyList<InputToken> seq, IReadOnlyList<InputToken> prefix)
    {
        if (prefix.Count > seq.Count) return false;
        for (int i = 0; i < prefix.Count; i++)
            if (seq[i] != prefix[i]) return false;
        return true;
    }

    // Every no-immediate-repeat combo of length MinLength..MaxLength.
    private static IEnumerable<List<InputToken>> AllCombos()
    {
        for (int len = MinLength; len <= MaxLength; len++)
            foreach (var c in Sequences(len, new List<InputToken>()))
                yield return c;
    }

    private static IEnumerable<List<InputToken>> Sequences(int len, List<InputToken> prefix)
    {
        if (prefix.Count == len)
        {
            yield return new List<InputToken>(prefix);
            yield break;
        }
        foreach (var token in Vocabulary)
        {
            if (prefix.Count > 0 && prefix[prefix.Count - 1] == token) continue; // no immediate repeat
            prefix.Add(token);
            foreach (var c in Sequences(len, prefix)) yield return c;
            prefix.RemoveAt(prefix.Count - 1);
        }
    }

    private static string Key(IReadOnlyList<InputToken> combo) => string.Join(",", combo);

    /// <summary>Parse token-name strings (as stored/sent) back into a combo.</summary>
    public static IReadOnlyList<InputToken> Parse(IEnumerable<string> tokens)
    {
        var combo = new List<InputToken>();
        if (tokens is null) return combo;
        foreach (var t in tokens)
            if (System.Enum.TryParse<InputToken>(t, ignoreCase: true, out var parsed))
                combo.Add(parsed);
        return combo;
    }
}
