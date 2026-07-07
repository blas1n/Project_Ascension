using Microsoft.Extensions.AI;
using ProjectAscension.SkillForge;

namespace ProjectAscension.Api.Services;

/// <summary>
/// LLM-backed effect-graph composer (ADR 0007) — provider-agnostic via Microsoft.Extensions.AI
/// <see cref="IChatClient"/>. Asks the model for the skill's STRUCTURE as a graph JSON, parses it,
/// and validates it against the power budget; the model owns structure/tiers, the engine owns the
/// numbers (ADR 0002).
///
/// The graph is additive to the primitive skill for this phase, so an unparseable/over-budget/
/// timed-out result returns null (the skill still ships via its primitives — no defer). A couple
/// of internal retries are taken first, since movement skills rely on the graph at runtime.
/// </summary>
public sealed class LlmEffectGraphComposer : IEffectGraphComposer
{
    private const int MaxAttempts = 2;

    private readonly IChatClient _chat;
    private readonly LlmComposerOptions _options;
    private readonly ILogger<LlmEffectGraphComposer> _logger;

    public LlmEffectGraphComposer(IChatClient chat, LlmComposerOptions options, ILogger<LlmEffectGraphComposer> logger)
    {
        _chat = chat;
        _options = options;
        _logger = logger;
    }

    public async Task<SkillGraphComposition?> ComposeAsync(EffectGraphRequest request, CancellationToken ct = default)
    {
        var prompt = SkillGraphPrompt.Build(request.Theme, request.Profile, request.Budget, request.Avoid, request.Lineage);

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_options.Timeout);
            try
            {
                var options = new ChatOptions
                {
                    ResponseFormat = ChatResponseFormat.Json,
                    Seed = request.Seed + attempt, // vary the retry so it doesn't repeat a bad graph
                    Temperature = 0.7f,
                };
                var response = await _chat.GetResponseAsync(prompt, options, cts.Token);

                var comp = EffectGraphJson.ParseComposition(response.Text);
                if (comp is not null && EffectGraphValidator.Validate(comp.Graph, request.Budget).IsValid)
                    return comp;

                _logger.LogDebug("LLM skill composition unparseable/invalid (attempt {Attempt}); retrying.", attempt + 1);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw; // external cancellation (shutdown) — propagate
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("LLM effect graph timed out after {Timeout}.", _options.Timeout);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM effect graph call failed.");
            }
        }

        return null; // graph is the sole artifact — a null defers the discovery (ADR 0002)
    }
}
