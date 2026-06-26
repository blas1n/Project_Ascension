#nullable enable
using System;

namespace ProjectAscension.Domain.Entities
{
    /// <summary>
    /// A directed edge in the discovery graph: a discovery (<see cref="ChildDiscoveryId"/>)
    /// was built on a prior owned discovery (<see cref="ParentDiscoveryId"/>).
    /// Discoveries form a graph, not a tree — one discovery can have several parents
    /// and feed several children (discovery.md 발견 그래프 / 발견 계보). Permanently
    /// recorded: "발견은 다음 발견의 시작."
    /// </summary>
    public class DiscoveryLineage
    {
        public Guid ChildDiscoveryId { get; set; }
        public Guid ParentDiscoveryId { get; set; }

        public Discovery? Child { get; set; }
        public Discovery? Parent { get; set; }
    }
}
