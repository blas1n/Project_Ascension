using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Kind).HasConversion<string>();
        builder.Property(c => c.Purpose).HasConversion<string>();
        builder.Property(c => c.Status).HasConversion<string>();
        builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
    }
}
