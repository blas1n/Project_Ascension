namespace ProjectAscension.SkillForge;

/// <summary>
/// Result of forging a skill: the AI composes, the validator disposes. On
/// repeated invalid output the content is <c>Deferred</c> (no fallback skill —
/// ADR 0002 core 2); the discovery's fact is already fixed, so deferral only
/// delays the (hidden) content.
/// </summary>
public sealed record CompositionOutcome(bool Forged, SkillComposition? Skill, ValidationResult LastValidation, int Attempts)
{
    public static CompositionOutcome Success(SkillComposition skill, ValidationResult validation, int attempts) =>
        new(true, skill, validation, attempts);

    public static CompositionOutcome Deferred(ValidationResult validation, int attempts) =>
        new(false, null, validation, attempts);
}

/// <summary>
/// Drives the compose → validate → retry loop (ADR 0002 core 3). The composer
/// proposes; <see cref="CompositionValidator"/> disposes (whitelist + power
/// budget). Invalid output is retried up to <paramref name="maxAttempts"/>; if
/// still invalid the content is deferred — there is no deterministic fallback,
/// preserving the uniqueness of every discovery.
/// </summary>
public static class CompositionPipeline
{
    public static async Task<CompositionOutcome> ForgeAsync(
        CompositionRequest request,
        ISkillComposer composer,
        int maxAttempts = 3,
        CancellationToken ct = default)
    {
        if (maxAttempts < 1) maxAttempts = 1;

        ValidationResult last = ValidationResult.Fail(CompositionError.EmptyComposition);
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var skill = await composer.ComposeAsync(request, ct).ConfigureAwait(false);
            last = CompositionValidator.Validate(skill, request.Budget);
            if (last.IsValid)
                return CompositionOutcome.Success(skill, last, attempt);
        }
        return CompositionOutcome.Deferred(last, maxAttempts);
    }
}
