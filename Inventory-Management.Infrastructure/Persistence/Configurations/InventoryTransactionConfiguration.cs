using Inventory_Management.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management.Infrastructure.Persistence.Configurations;

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(t => t.ReferenceType)
            .HasMaxLength(50);

        builder.Property(t => t.Notes)
            .HasMaxLength(1000);

        builder.Property(t => t.CreatedBy)
            .HasMaxLength(450);

        builder.HasIndex(t => t.ProductId);
        builder.HasIndex(t => t.CreatedAt);
        builder.HasIndex(t => t.ReferenceId);
        builder.HasIndex(t => t.Type);
        builder.HasIndex(t => t.CreatedBy);

        builder.HasOne(t => t.Product)
            .WithMany(p => p.InventoryTransactions)
            .HasForeignKey(t => t.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
