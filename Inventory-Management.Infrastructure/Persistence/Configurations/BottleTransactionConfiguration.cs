using Inventory_Management.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management.Infrastructure.Persistence.Configurations;

public class BottleTransactionConfiguration : IEntityTypeConfiguration<BottleTransaction>
{
    public void Configure(EntityTypeBuilder<BottleTransaction> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasOne(x => x.BottleType)
            .WithMany(x => x.BottleTransactions)
            .HasForeignKey(x => x.BottleTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.BottleTransactions)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.Property(x => x.DepositAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.ReferenceType).HasMaxLength(100);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
    }
}
