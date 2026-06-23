#nullable enable
using System;

namespace ProjectAscension.Contracts.Requests
{
    public record UpdateLoadoutRequest(Guid? LeftItemId, Guid? RightItemId);
}
