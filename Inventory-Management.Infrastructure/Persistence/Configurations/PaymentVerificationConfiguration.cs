using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management.Infrastructure.Persistence.Configurations;

public class PaymentVerificationConfiguration : IEntityTypeConfiguration<PaymentVerification>
{
    public void Configure(EntityTypeBuilder<PaymentVerification> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Provider)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(v => v.BankCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(v => v.SubmittedReference)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(v => v.NormalizedReference)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(v => v.VerifiedReference)
            .HasMaxLength(250);

        builder.Property(v => v.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(v => v.ExpectedAmount)
            .HasColumnType("numeric(18,2)");

        builder.Property(v => v.VerifiedAmount)
            .HasColumnType("numeric(18,2)");

        builder.Property(v => v.PayerName)
            .HasMaxLength(200);

        builder.Property(v => v.PayerAccount)
            .HasMaxLength(100);

        builder.Property(v => v.ReceiverName)
            .HasMaxLength(200);

        builder.Property(v => v.ReceiverAccount)
            .HasMaxLength(100);

        builder.Property(v => v.VerifiedByUserId)
            .HasMaxLength(450);

        builder.Property(v => v.VerifiedByUserName)
            .HasMaxLength(200);

        builder.Property(v => v.ReviewedByUserId)
            .HasMaxLength(450);

        builder.Property(v => v.ReviewedByUserName)
            .HasMaxLength(200);

        builder.Property(v => v.FailureCode)
            .HasMaxLength(100);

        builder.Property(v => v.FailureMessage)
            .HasMaxLength(1000);

        builder.Property(v => v.ProviderTransactionId)
            .HasMaxLength(250);

        builder.Property(v => v.SourceUrl)
            .HasMaxLength(500);

        builder.Property(v => v.ManualReviewNotes)
            .HasMaxLength(1000);

        builder.HasIndex(v => new { v.TenantId, v.Status });
        builder.HasIndex(v => new { v.TenantId, v.CreatedAtUtc });
        builder.HasIndex(v => v.SaleId);
        
        // Prevent duplicate applied/verified references per tenant and bank
        builder.HasIndex(v => new { v.TenantId, v.BankCode, v.NormalizedReference })
            .IsUnique()
            .HasFilter("\"Status\" = 2");

        builder.HasOne(v => v.Sale)
            .WithMany()
            .HasForeignKey(v => v.SaleId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.PaymentDestination)
            .WithMany()
            .HasForeignKey(v => v.PaymentDestinationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
