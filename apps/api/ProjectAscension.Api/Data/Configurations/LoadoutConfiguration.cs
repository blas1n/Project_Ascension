using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class LoadoutConfiguration : IEntityTypeConfiguration<Loadout>
{
    public void Configure(EntityTypeBuilder<Loadout> builder)
    {
        builder.HasKey(l => l.ActorId);
        builder.HasOne(l => l.Actor).WithOne()
            .HasForeignKey<Loadout>(l => l.ActorId);
    }
}
