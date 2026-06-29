using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class NpcConfiguration : IEntityTypeConfiguration<NPC>
{
    private static readonly System.DateTime SeedTime = new(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
    private static readonly System.Guid City = System.Guid.Parse("33333333-3333-3333-3333-333333333333");

    public void Configure(EntityTypeBuilder<NPC> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Name).IsRequired().HasMaxLength(64);
        builder.Property(n => n.Role).HasMaxLength(32);

        // The city's staff (MVP NPCs: shop, guard, contract clerk) — static, named presence.
        builder.HasData(
            new NPC
            {
                Id = System.Guid.Parse("a0000001-0000-0000-0000-000000000001"),
                Name = "Quartermaster Hale",
                Role = "Shopkeeper",
                HomeRegionId = City,
                CurrentRegionId = City,
                Alive = true,
                CreatedAt = SeedTime,
            },
            new NPC
            {
                Id = System.Guid.Parse("a0000002-0000-0000-0000-000000000002"),
                Name = "Serjeant Bran",
                Role = "Guard",
                HomeRegionId = City,
                CurrentRegionId = City,
                Alive = true,
                CreatedAt = SeedTime,
            },
            new NPC
            {
                Id = System.Guid.Parse("a0000003-0000-0000-0000-000000000003"),
                Name = "Survey Clerk Mira",
                Role = "Contract Clerk",
                HomeRegionId = City,
                CurrentRegionId = City,
                Alive = true,
                CreatedAt = SeedTime,
            });
    }
}
