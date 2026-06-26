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
}
