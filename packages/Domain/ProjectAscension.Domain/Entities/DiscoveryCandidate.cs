namespace ProjectAscension.Domain.Entities;

public class DiscoveryCandidate
{
    public Guid Id { get; set; }
    public string CandidateKey { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string RequiredContextJson { get; set; } = "{}";
    public int RequiredProgress { get; set; }
    public string Rarity { get; set; } = "Common";
    public DateTime CreatedAt { get; set; }
}
