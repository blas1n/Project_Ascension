#nullable enable
using System;
using System.Collections.Generic;

namespace ProjectAscension.Contracts.Responses
{
    /// <summary>One ancestor in a discovery's lineage.</summary>
    public record LineageEntry(Guid DiscoveryId, string Name);

    /// <summary>A discovery's recorded lineage — the prior discoveries it was built
    /// on, nearest first (discovery.md 발견 계보). Permanently recorded.</summary>
    public record DiscoveryLineageResponse(Guid DiscoveryId, IReadOnlyList<LineageEntry> Ancestors);
}
