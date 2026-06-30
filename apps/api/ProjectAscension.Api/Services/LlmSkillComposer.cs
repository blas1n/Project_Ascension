using Microsoft.Extensions.AI;
using ProjectAscension.SkillForge;

namespace ProjectAscension.Api.Services;

/// <summary>
/// LLM-backed skill composer — provider-agnostic via Microsoft.Extensions.AI
/// <see cref="IChatClient"/> (Ollama / OpenAI / Claude by config). Prompts the model
/// for a JSON composition, parses it, and packs it into the power budget. The model
/// supplies the concept (which primitives + name/lore); the engine owns the numbers
/// (<see cref="BudgetPacker"/>), so a model that ignores the budget still yields a
/// valid skill.
///
/// Reliability: the request asks for a JSON response format (so the provider
/// constrains output to JSON), and every call is bounded by a timeout. Unparseable,
/// timed-out, or failed calls return an invalid composition so the pipeline retries
/// — and defers after maxAttempts (no fallback, ADR 0002).
/// </summary>
public class LlmSkillComposer : ISkillComposer
{
    private static readonly SkillComposition Invalid =
        new(string.Empty, string.Empty, Array.Empty<ComposedPrimitive>());

    private readonly IChatClient _chat;
    private readonly LlmComposerOptions _options;
    private readonly ILogger<LlmSkillComposer> _logger;

    public LlmSkillComposer(IChatClient chat, LlmComposerOptions options, ILogger<LlmSkillComposer> logger)
    {
        _chat = chat;
        _options = options;
        _logger = logger;
    }

    public async Task<SkillComposition> ComposeAsync(CompositionRequest request, CancellationToken ct = default)
    {
        var prompt = SkillCompositionPrompt.Build(request);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_options.Timeout);

        try
        {
            // Seed is derived from the discovery's identity, so the composition is frozen and
            // reproducible (ADR 0002) yet distinct per discovery — two near-identical seeds no
            // longer collapse to one identical skill. Temperature lets the behavior profile
            // actually steer the output rather than snapping to a canonical answer.
            var options = new ChatOptions
            {
                ResponseFormat = ChatResponseFormat.Json,
                Seed = request.Seed,
                Temperature = 0.7f,
            };
            var response = await _chat.GetResponseAsync(prompt, options, cts.Token);

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
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // external cancellation (e.g. shutdown) — propagate
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("LLM composition timed out after {Timeout}; deferring.", _options.Timeout);
            return Invalid;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM composition call failed; deferring.");
            return Invalid;
        }
    }
}
