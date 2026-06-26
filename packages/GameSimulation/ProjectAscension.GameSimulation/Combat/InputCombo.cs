using System;
using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>Parses the API's invocation-combo strings (e.g. "Jump", "RightClick")
    /// into <see cref="InputToken"/>s the recognizer matches. Unknown tokens are
    /// dropped.</summary>
    public static class InputCombo
    {
        public static IReadOnlyList<InputToken> Parse(IEnumerable<string> tokens)
        {
            var combo = new List<InputToken>();
            if (tokens == null) return combo;
            foreach (var token in tokens)
                if (Enum.TryParse<InputToken>(token, ignoreCase: true, out var parsed))
                    combo.Add(parsed);
            return combo;
        }
    }
}
