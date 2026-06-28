using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
{
    public void Configure(EntityTypeBuilder<Settlement> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever(); // singleton row
        builder.Property(s => s.Name).HasMaxLength(64);

        // The frontier outpost starts undeveloped — the player grows it by delivering resources.
        builder.HasData(new Settlement { Id = 1, Name = "Frontier Outpost" });
    }
}
