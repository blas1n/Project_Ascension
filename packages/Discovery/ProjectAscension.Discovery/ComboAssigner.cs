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
/// to invoke it. The combo is decided by the rule engine (deterministic, not by the
/// AI and not tied to the behaviors that discovered it): the discovery is the
/// "incantation"'s seed, so the same discovery always maps to the same combo, and a
/// single-behavior discovery (double jump) gets a combo just like a multi-behavior one
/// — all active non-weapon skills are invoked the same way.
/// </summary>
public static class ComboAssigner
{
    private static readonly InputToken[] Vocabulary =
        { InputToken.Jump, InputToken.Dodge, InputToken.LeftClick, InputToken.RightClick };

    public const int MinLength = 2;
    public const int MaxLength = 4;

    public static IReadOnlyList<InputToken> Assign(string seed)
    {
        uint hash = Fnv(seed ?? string.Empty);
        int length = MinLength + (int)(hash % (uint)(MaxLength - MinLength + 1));

        var combo = new List<InputToken>(length);
        for (int i = 0; i < length; i++)
        {
            hash = (hash ^ (uint)(i + 1)) * 16777619u;
            int index = (int)(hash % (uint)Vocabulary.Length);
            var token = Vocabulary[index];
            // Avoid a trivial immediate repeat (e.g. Jump, Jump) for a cleaner combo.
            if (combo.Count > 0 && token == combo[combo.Count - 1])
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
