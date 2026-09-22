using Inventory_Management.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management.Infrastructure.Persistence.Configurations;

public class BottleTypeConfiguration : IEntityTypeConfiguration<BottleType>
{
    public void Configure(EntityTypeBuilder<BottleType> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.DepositAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Capacity).HasMaxLength(50);
        builder.Property(x => x.Material).HasMaxLength(50);
    }
}
