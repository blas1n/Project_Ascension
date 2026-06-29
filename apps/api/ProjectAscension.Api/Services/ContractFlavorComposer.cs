using Microsoft.Extensions.AI;
using ProjectAscension.Domain.Enums;
using ProjectAscension.SkillForge;

namespace ProjectAscension.Api.Services;

/// <summary>A contract's flavor text — title and description. AI fills these for a
/// player-issued contract; the objective and reward stay deterministic (ADR 0002 — AI
/// writes flavor, never numbers).</summary>
public record ContractFlavor(string Title, string Description);

public interface IContractFlavorComposer
{
    /// <summary>Compose a title/description for a prospective contract. The fallbacks (the
    /// deterministic template) are returned whenever AI is unavailable or unusable.</summary>
    Task<ContractFlavor> ComposeAsync(
        ContractPurpose purpose, string? target, int count,
        string fallbackTitle, string fallbackDescription, CancellationToken ct = default);
}

/// <summary>Deterministic flavor — just the template. Used offline / in CI / by default.</summary>
public class StubContractFlavorComposer : IContractFlavorComposer
{
    public Task<ContractFlavor> ComposeAsync(
        ContractPurpose purpose, string? target, int count,
        string fallbackTitle, string fallbackDescription, CancellationToken ct = default)
        => Task.FromResult(new ContractFlavor(fallbackTitle, fallbackDescription));
}

/// <summary>LLM-backed flavor — the model writes a posting from the objective facts. Any
/// failure (timeout, unparseable, error) returns the template fallback, so issuing never
/// breaks on the model. Numbers are NOT taken from the model — the engine owns those.</summary>
public class LlmContractFlavorComposer : IContractFlavorComposer
{
    private readonly IChatClient _chat;
    private readonly LlmComposerOptions _options;
    private readonly ILogger<LlmContractFlavorComposer> _logger;

    public LlmContractFlavorComposer(IChatClient chat, LlmComposerOptions options, ILogger<LlmContractFlavorComposer> logger)
    {
        _chat = chat;
        _options = options;
        _logger = logger;
    }

    public async Task<ContractFlavor> ComposeAsync(
        ContractPurpose purpose, string? target, int count,
        string fallbackTitle, string fallbackDescription, CancellationToken ct = default)
    {
        string objective = string.IsNullOrEmpty(target)
            ? $"{purpose}, quantity {count}"
            : $"{purpose} of {target} monsters, quantity {count}";
        var prompt =
            "You write short contract postings for a frontier expedition guild in a dark-fantasy world. " +
            $"Objective: {objective}. " +
            "Respond ONLY as JSON: {\"title\": \"...\", \"description\": \"...\"}. " +
            "Title: at most 5 words, evocative. Description: one vivid sentence, in-world. " +
            "Do not invent rewards or numbers beyond the stated quantity.";

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_options.Timeout);
        try
        {
            var options = new ChatOptions { ResponseFormat = ChatResponseFormat.Json };
            var response = await _chat.GetResponseAsync(prompt, options, cts.Token);
            var parsed = TryParse(response.Text);
            return parsed ?? new ContractFlavor(fallbackTitle, fallbackDescription);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM contract flavor failed; using the template.");
            return new ContractFlavor(fallbackTitle, fallbackDescription);
        }
    }

    private static ContractFlavor? TryParse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(text);
            var root = doc.RootElement;
            if (!root.TryGetProperty("title", out var t) || !root.TryGetProperty("description", out var d)) return null;
            var title = t.GetString();
            var description = d.GetString();
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description)) return null;
            return new ContractFlavor(Clamp(title!, 60), Clamp(description!, 180));
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static string Clamp(string s, int max)
    {
        s = s.Trim();
        return s.Length <= max ? s : s.Substring(0, max);
    }
}
