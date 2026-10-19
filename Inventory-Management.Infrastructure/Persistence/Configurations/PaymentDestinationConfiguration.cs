using Inventory_Management.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management.Infrastructure.Persistence.Configurations;

public class PaymentDestinationConfiguration : IEntityTypeConfiguration<PaymentDestination>
{
    public void Configure(EntityTypeBuilder<PaymentDestination> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(d => d.BankCode)
            .HasMaxLength(50);

        builder.Property(d => d.BankName)
            .HasMaxLength(150);

        builder.Property(d => d.AccountNumber)
            .HasMaxLength(100);

        builder.Property(d => d.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(d => d.AccountHolderName)
            .HasMaxLength(200);

        builder.Property(d => d.CreatedByUserId)
            .HasMaxLength(450);

        builder.HasIndex(d => new { d.TenantId, d.IsActive });
        builder.HasIndex(d => new { d.TenantId, d.IsDefault });

        builder.HasOne(d => d.CreatedByUser)
            .WithMany()
            .HasForeignKey(d => d.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
