using Microsoft.Extensions.AI;
using ProjectAscension.SkillForge;

namespace ProjectAscension.Api.Services;

/// <summary>
/// LLM-backed skill composer — provider-agnostic via Microsoft.Extensions.AI
/// <see cref="IChatClient"/> (Ollama / OpenAI / Claude by config). Prompts the model
/// for a JSON composition, parses it, and packs it into the power budget. The model
/// supplies the concept (which primitives + name/lore); the engine owns the numbers
/// (<see cref="BudgetPacker"/>), so a model that ignores the budget still yields a
/// valid skill. Unparseable or failed calls return an invalid composition so the
/// pipeline retries — and defers after maxAttempts (no fallback, ADR 0002).
/// </summary>
public class LlmSkillComposer : ISkillComposer
{
    private static readonly SkillComposition Invalid =
        new(string.Empty, string.Empty, Array.Empty<ComposedPrimitive>());

    private readonly IChatClient _chat;
    private readonly ILogger<LlmSkillComposer> _logger;

    public LlmSkillComposer(IChatClient chat, ILogger<LlmSkillComposer> logger)
    {
        _chat = chat;
        _logger = logger;
    }

    public async Task<SkillComposition> ComposeAsync(CompositionRequest request, CancellationToken ct = default)
    {
        var prompt = SkillCompositionPrompt.Build(request);
        try
        {
            var response = await _chat.GetResponseAsync(prompt, cancellationToken: ct);
            var parsed = SkillCompositionParser.TryParse(response.Text);
            if (parsed is null)
            {
                _logger.LogDebug("LLM returned unparseable skill JSON; will retry.");
                return Invalid;
            }

            // The model proposes; the rule engine packs it into the budget.
            var packed = BudgetPacker.Pack(parsed.Primitives, request.Budget);
            return new SkillComposition(parsed.Name, parsed.Description, packed);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM composition call failed; deferring.");
            return Invalid;
        }
    }
}
