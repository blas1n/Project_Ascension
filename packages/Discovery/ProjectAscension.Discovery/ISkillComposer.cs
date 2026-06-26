namespace ProjectAscension.SkillForge;

/// <summary>
/// Composes a skill for a triggered discovery. Implementations: a deterministic
/// stub (tests/offline) and an LLM-backed composer in the API shell (provider via
/// Microsoft.Extensions.AI — Ollama/OpenAI/Claude). The composer proposes;
/// <see cref="CompositionValidator"/> disposes (whitelist + power budget).
/// </summary>
public interface ISkillComposer
{
    Task<SkillComposition> ComposeAsync(CompositionRequest request, CancellationToken ct = default);
}
