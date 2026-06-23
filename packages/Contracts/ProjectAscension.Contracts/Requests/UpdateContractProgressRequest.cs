#nullable enable
using System;

namespace ProjectAscension.Contracts.Requests
{
    public record UpdateContractProgressRequest(Guid ActorId, int ProgressCount);
}
