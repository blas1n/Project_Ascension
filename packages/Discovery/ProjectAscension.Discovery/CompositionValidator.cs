namespace ProjectAscension.SkillForge;

public enum CompositionError
{
    None,
    MissingName,
    EmptyComposition,
    UnknownPrimitive,
    InvalidMagnitude,
    OverBudget,
}

/// <summary>Outcome of validating a composition. <see cref="TotalCost"/> is the
/// summed power cost when computable (e.g. on an over-budget failure).</summary>
public sealed record ValidationResult(bool IsValid, CompositionError Error, int TotalCost)
{
    public static ValidationResult Ok(int totalCost) => new(true, CompositionError.None, totalCost);
    public static ValidationResult Fail(CompositionError error, int totalCost = 0) => new(false, error, totalCost);
}

/// <summary>
/// The deterministic gate on AI output: every primitive must be whitelisted,
/// magnitudes sane, and total power within budget. The AI composes; this keeps
/// balance server-authoritative (ADR 0002 core 3). On failure the pipeline retries
/// — there is no fallback skill.
/// </summary>
public static class CompositionValidator
{
    public const int MaxMagnitude = 5;

    public static ValidationResult Validate(SkillComposition? composition, PowerBudget budget)
    {
        if (composition is null) return ValidationResult.Fail(CompositionError.EmptyComposition);
        if (string.IsNullOrWhiteSpace(composition.Name)) return ValidationResult.Fail(CompositionError.MissingName);
        if (composition.Primitives is null || composition.Primitives.Count == 0)
            return ValidationResult.Fail(CompositionError.EmptyComposition);

        int total = 0;
        foreach (var p in composition.Primitives)
        {
            if (!PrimitiveCatalog.IsKnown(p.Kind)) return ValidationResult.Fail(CompositionError.UnknownPrimitive);
            if (p.Magnitude < 1 || p.Magnitude > MaxMagnitude) return ValidationResult.Fail(CompositionError.InvalidMagnitude);
            total += PrimitiveCatalog.BaseCostOf(p.Kind) * p.Magnitude;
        }

        return total > budget.Total
            ? ValidationResult.Fail(CompositionError.OverBudget, total)
            : ValidationResult.Ok(total);
    }
}
