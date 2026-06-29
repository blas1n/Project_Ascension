using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class PlayerProfileConfiguration : IEntityTypeConfiguration<PlayerProfile>
{
    public void Configure(EntityTypeBuilder<PlayerProfile> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever(); // singleton row

        builder.HasData(new PlayerProfile { Id = 1 }); // a fresh save
    }
}
