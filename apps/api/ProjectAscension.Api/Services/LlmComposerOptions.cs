namespace ProjectAscension.Api.Services;

/// <summary>Tunables for the LLM-backed composer.</summary>
public sealed class LlmComposerOptions
{
    /// <summary>Per-call ceiling. A model that hangs past this is abandoned and the
    /// content deferred (retried on a later pass) — the worker never blocks.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
}
