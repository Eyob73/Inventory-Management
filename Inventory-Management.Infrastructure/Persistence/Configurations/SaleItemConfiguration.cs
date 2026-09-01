using Inventory_Management.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management.Infrastructure.Persistence.Configurations;

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.HasKey(si => si.Id);

        builder.Property(si => si.ProductName)
            .HasMaxLength(100);

        builder.Property(si => si.SKU)
            .HasMaxLength(100);

        builder.Property(si => si.UnitPrice)
            .HasColumnType("numeric(18,2)");

        builder.Property(si => si.DiscountAmount)
            .HasColumnType("numeric(18,2)");

        builder.Property(si => si.Subtotal)
            .HasColumnType("numeric(18,2)");

        builder.Property(si => si.TotalPrice)
            .HasColumnType("numeric(18,2)");

        builder.HasOne(si => si.Sale)
            .WithMany(s => s.SaleItems)
            .HasForeignKey(si => si.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(si => si.Product)
            .WithMany()
            .HasForeignKey(si => si.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(si => si.ProductId);
        builder.HasIndex(si => si.SaleId);
    }
}
