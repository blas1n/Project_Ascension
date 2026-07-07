namespace ProjectAscension.SkillForge;

public enum CompositionError
{
    None,
    MissingName,
    EmptyComposition,
    UnknownPrimitive,
    InvalidMagnitude,
    InvalidParameter,
    OverBudget,
}

/// <summary>Outcome of validating a composition. <see cref="TotalCost"/> is the summed power
/// cost when computable. Shared by <see cref="EffectGraphValidator"/> (the flat-primitive
/// CompositionValidator was retired with primitive generation — ADR 0007 Phase 4c).</summary>
public sealed record ValidationResult(bool IsValid, CompositionError Error, int TotalCost)
{
    public static ValidationResult Ok(int totalCost) => new(true, CompositionError.None, totalCost);
    public static ValidationResult Fail(CompositionError error, int totalCost = 0) => new(false, error, totalCost);
}
