using System.Text.Json;

namespace ProjectAscension.SkillForge;

/// <summary>
/// Parses an LLM response into a <see cref="SkillComposition"/>. Tolerates prose or
/// markdown fences around the JSON by extracting the first <c>{ … }</c> object.
/// Returns null on no parseable object, malformed JSON, or any unknown primitive
/// kind — the caller turns that into an invalid composition so the pipeline retries
/// (compose → validate → retry; no fallback).
/// </summary>
public static class SkillCompositionParser
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    private sealed record Dto(string? Name, string? Description, PrimitiveDto[]? Primitives);
    private sealed record PrimitiveDto(string? Kind, int Magnitude);

    public static SkillComposition? TryParse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (!TryExtractObject(text, out var json)) return null;

        Dto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<Dto>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }

        if (dto?.Primitives is null) return null;

        var primitives = new List<ComposedPrimitive>(dto.Primitives.Length);
        foreach (var p in dto.Primitives)
        {
            if (p?.Kind is null) return null;
            if (!Enum.TryParse<PrimitiveKind>(p.Kind, ignoreCase: true, out var kind)) return null;
            primitives.Add(new ComposedPrimitive(kind, p.Magnitude));
        }

        return new SkillComposition(dto.Name ?? string.Empty, dto.Description ?? string.Empty, primitives);
    }

    private static bool TryExtractObject(string text, out string json)
    {
        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            json = text.Substring(start, end - start + 1);
            return true;
        }
        json = string.Empty;
        return false;
    }
}
