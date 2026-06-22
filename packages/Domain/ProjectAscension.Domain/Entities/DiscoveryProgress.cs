namespace ProjectAscension.Domain.Entities;

public class DiscoveryProgress
{
    public Guid Id { get; set; }
    public Guid ActorId { get; set; }
    public Guid DiscoveryCandidateId { get; set; }
    public int Progress { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; }

    public Actor? Actor { get; set; }
    public DiscoveryCandidate? Candidate { get; set; }
}
