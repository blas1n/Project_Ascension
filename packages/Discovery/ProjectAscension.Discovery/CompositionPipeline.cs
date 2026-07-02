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

        // Primitive-combinations already taken — start from the lineage the RAG gave us, and
        // grow it as we reject duplicates, so the retry actively steers AWAY from them.
        var avoid = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var art in request.Lineage ?? Array.Empty<PriorArt>())
            avoid.Add(KindSignature(art.Primitives));
        // ...and every skill the actor has already composed (Avoid), so two discoveries on
        // DIFFERENT behavior lines can't land on the same effect — the lineage alone only
        // covers the same line, which let cross-line duplicates slip through.
        foreach (var sig in request.Avoid ?? Array.Empty<string>())
            if (!string.IsNullOrEmpty(sig)) avoid.Add(sig);

        ValidationResult last = ValidationResult.Fail(CompositionError.EmptyComposition);
        SkillComposition? lastValid = null;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            // Vary the seed and pass the avoid-set each attempt so a retry produces something
            // genuinely different, not the same output the model converged on.
            var attemptReq = request with { Seed = request.Seed + attempt, Avoid = avoid.ToList() };
            var skill = await composer.ComposeAsync(attemptReq, ct).ConfigureAwait(false);
            last = CompositionValidator.Validate(skill, request.Budget);
            if (!last.IsValid) continue;

            var signature = KindSignature(skill.Primitives);
            if (!avoid.Contains(signature))
                return CompositionOutcome.Success(skill, last, attempt); // distinct — done

            // A duplicate of an existing skill: remember it, forbid it next attempt, keep it
            // only as a last resort so we never defer a discovery to nothing.
            avoid.Add(signature);
            lastValid = skill;
        }

        // Every attempt duplicated an existing skill — accept the last valid one rather than
        // leave the discovery empty (rare; the retry usually finds a distinct composition).
        return lastValid is not null
            ? CompositionOutcome.Success(lastValid, last, maxAttempts)
            : CompositionOutcome.Deferred(last, maxAttempts);
    }

    // The distinct primitive KINDS a skill is built from (order/magnitude-independent) — two
    // skills sharing this signature are the "same" mechanically, i.e. duplicates. Public so
    // the composition service can seed the actor-wide Avoid set with the same signature.
    public static string KindSignature(IReadOnlyList<ComposedPrimitive> primitives)
        => string.Join(",", primitives.Select(p => p.Kind).Distinct().OrderBy(k => k.ToString(), StringComparer.Ordinal));
}
