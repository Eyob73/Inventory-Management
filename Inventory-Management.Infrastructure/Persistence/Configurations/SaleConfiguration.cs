using Inventory_Management.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management.Infrastructure.Persistence.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.SaleNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(s => s.SaleNumber)
            .IsUnique();

        builder.HasIndex(s => s.SaleDate);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.CustomerId);

        builder.Property(s => s.Subtotal)
            .HasColumnType("numeric(18,2)");

        builder.Property(s => s.DiscountAmount)
            .HasColumnType("numeric(18,2)");

        builder.Property(s => s.TaxAmount)
            .HasColumnType("numeric(18,2)");

        builder.Property(s => s.TotalAmount)
            .HasColumnType("numeric(18,2)");

        builder.Property(s => s.AmountReceived)
            .HasColumnType("numeric(18,2)");

        builder.Property(s => s.ChangeAmount)
            .HasColumnType("numeric(18,2)");

        builder.Property(s => s.PaymentMethod)
            .HasMaxLength(50);

        builder.Property(s => s.Status)
            .HasMaxLength(50);

        builder.Property(s => s.CustomerName)
            .HasMaxLength(200);

        builder.Property(s => s.CashierName)
            .HasMaxLength(200);

        builder.HasOne(s => s.Customer)
            .WithMany(c => c.Sales)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(s => s.SaleItems)
            .WithOne(si => si.Sale)
            .HasForeignKey(si => si.SaleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
