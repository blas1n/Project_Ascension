#nullable enable
using System;
using ProjectAscension.Domain.Enums;
namespace ProjectAscension.Domain.Entities
{
    public class Monster
    {
        public Guid Id { get; set; }
        public Guid SpeciesId { get; set; }
        public Guid RegionId { get; set; }
        public MonsterTier Tier { get; set; }
        public bool Alive { get; set; } = true;
        public DateTime SpawnedAt { get; set; }

        public MonsterSpecies? Species { get; set; }
        public Region? Region { get; set; }
    }
}
