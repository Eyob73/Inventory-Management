using Inventory_Management.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management.Infrastructure.Persistence.Configurations;

public class CustomerBottleBalanceConfiguration : IEntityTypeConfiguration<CustomerBottleBalance>
{
    public void Configure(EntityTypeBuilder<CustomerBottleBalance> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasOne(x => x.Customer)
            .WithMany(x => x.BottleBalances)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BottleType)
            .WithMany(x => x.CustomerBottleBalances)
            .HasForeignKey(x => x.BottleTypeId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.Property(x => x.TotalDeposit).HasColumnType("decimal(18,2)");
    }
}
