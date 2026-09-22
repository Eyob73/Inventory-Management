using Inventory_Management.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management.Infrastructure.Persistence.Configurations;

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PurchaseNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.PurchaseNumber)
            .IsUnique();

        builder.HasIndex(p => p.PurchaseDate);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.SupplierId);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.Notes)
            .HasMaxLength(1000);

        builder.Property(p => p.CreatedBy)
            .HasMaxLength(450);

        builder.Property(p => p.TotalAmount)
            .HasColumnType("numeric(18,2)");

        builder.HasOne(p => p.Supplier)
            .WithMany(s => s.Purchases)
            .HasForeignKey(p => p.SupplierId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.PurchaseItems)
            .WithOne(pi => pi.Purchase)
            .HasForeignKey(pi => pi.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
