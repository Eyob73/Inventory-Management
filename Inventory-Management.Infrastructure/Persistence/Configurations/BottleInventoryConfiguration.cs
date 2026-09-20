using Inventory_Management.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management.Infrastructure.Persistence.Configurations;

public class BottleInventoryConfiguration : IEntityTypeConfiguration<BottleInventory>
{
    public void Configure(EntityTypeBuilder<BottleInventory> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasOne(x => x.BottleType)
            .WithMany(x => x.BottleInventories)
            .HasForeignKey(x => x.BottleTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
