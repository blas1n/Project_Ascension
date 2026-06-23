#nullable enable
using System;

namespace ProjectAscension.Domain.Entities
{
    public class NPC
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid HomeRegionId { get; set; }
        public Guid CurrentRegionId { get; set; }
        public bool Alive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}
