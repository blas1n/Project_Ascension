#nullable enable
namespace ProjectAscension.Domain.Enums
{
    /// <summary>Lifecycle of a discovery's AI-created content (ADR 0002 fact/content
    /// separation). The fact is fixed instantly; the content starts Pending and is
    /// frozen to Ready once composed.</summary>
    public enum DiscoveryContentStatus { Pending, Ready }
}
