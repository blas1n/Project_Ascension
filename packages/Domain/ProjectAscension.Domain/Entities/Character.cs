#nullable enable
using System;

namespace ProjectAscension.Domain.Entities
{
    public class Character
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid OriginRegionId { get; set; }
        public Guid CurrentRegionId { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; }

        public Actor? Actor { get; set; }
        public Region? CurrentRegion { get; set; }
    }
}
