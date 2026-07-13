#nullable enable

namespace ProjectAscension.Contracts.Requests
{
    /// <summary>Names a new character. The server mints the Character and its Actor — the
    /// identity every economy/discovery/contract check is keyed on (ADR 0014) — atomically,
    /// so the client never invents an actor id; it takes the one this request returns.</summary>
    public record CreateCharacterRequest(string Name);
}
