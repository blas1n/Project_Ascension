#nullable enable

namespace ProjectAscension.Contracts.Responses
{
    /// <summary>The server's calibrated reward for a prospective contract objective — a
    /// suggested fair value and the band the issuer may choose within. The UI shows this
    /// live as the player builds the objective, so issuing stays a choice (how generous),
    /// not balance math.</summary>
    public record ContractQuoteResponse(int SuggestedReward, int MinReward, int MaxReward);
}
