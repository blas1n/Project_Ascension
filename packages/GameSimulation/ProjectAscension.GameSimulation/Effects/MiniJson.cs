using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ProjectAscension.GameSimulation.Effects
{
    /// <summary>
    /// A tiny, dependency-free JSON reader — GameSimulation is compiled by Unity (C# 9, no
    /// System.Text.Json), so the effect-graph JSON the server serves is parsed by hand. Returns
    /// a plain object tree: <see cref="IDictionary{TKey,TValue}"/> for objects,
    /// <see cref="IList{T}"/> for arrays, string / double / bool / null for scalars. Total —
    /// any malformed input returns null (the caller then treats the skill as graphless).
    /// Not a general-purpose serializer; just enough to read our canonical graph shape.
    /// </summary>
    internal static class MiniJson
    {
        public static object Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                int i = 0;
                var value = ParseValue(json, ref i);
                SkipWhitespace(json, ref i);
                return i == json.Length ? value : null; // trailing garbage → reject
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static object ParseValue(string s, ref int i)
        {
            SkipWhitespace(s, ref i);
            char c = s[i];
            switch (c)
            {
                case '{': return ParseObject(s, ref i);
                case '[': return ParseArray(s, ref i);
                case '"': return ParseString(s, ref i);
                case 't': Expect(s, ref i, "true"); return true;
                case 'f': Expect(s, ref i, "false"); return false;
                case 'n': Expect(s, ref i, "null"); return null;
                default: return ParseNumber(s, ref i);
            }
        }

        private static Dictionary<string, object> ParseObject(string s, ref int i)
        {
            var obj = new Dictionary<string, object>();
            i++; // '{'
            SkipWhitespace(s, ref i);
            if (s[i] == '}') { i++; return obj; }
            while (true)
            {
                SkipWhitespace(s, ref i);
                string key = ParseString(s, ref i);
                SkipWhitespace(s, ref i);
                if (s[i] != ':') throw new FormatException();
                i++;
                obj[key] = ParseValue(s, ref i);
                SkipWhitespace(s, ref i);
                char c = s[i++];
                if (c == '}') return obj;
                if (c != ',') throw new FormatException();
            }
        }

        private static List<object> ParseArray(string s, ref int i)
        {
            var arr = new List<object>();
            i++; // '['
            SkipWhitespace(s, ref i);
            if (s[i] == ']') { i++; return arr; }
            while (true)
            {
                arr.Add(ParseValue(s, ref i));
                SkipWhitespace(s, ref i);
                char c = s[i++];
                if (c == ']') return arr;
                if (c != ',') throw new FormatException();
            }
        }

        private static string ParseString(string s, ref int i)
        {
            if (s[i] != '"') throw new FormatException();
            i++;
            var sb = new StringBuilder();
            while (true)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c == '\\')
                {
                    char e = s[i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u':
                            sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            i += 4;
                            break;
                        default: throw new FormatException();
                    }
                }
                else sb.Append(c);
            }
        }

        private static double ParseNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            return double.Parse(s.Substring(start, i - start), CultureInfo.InvariantCulture);
        }

        private static void Expect(string s, ref int i, string literal)
        {
            if (i + literal.Length > s.Length || s.Substring(i, literal.Length) != literal)
                throw new FormatException();
            i += literal.Length;
        }

        private static void SkipWhitespace(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }
    }
}
