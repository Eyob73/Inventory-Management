using System.Reflection;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Common;
using Inventory_Management.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Infrastructure.Persistence.Data;

public class AppDbContext : DbContext
{
    private readonly ICurrentTenant? _currentTenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenant? currentTenant = null)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Purchase> Purchases { get; set; }
    public DbSet<PurchaseItem> PurchaseItems { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleItem> SaleItems { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Apply global query filter for IMultiTenant entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IMultiTenant).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(SetTenantQueryFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    private void SetTenantQueryFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, IMultiTenant
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == (_currentTenant != null ? _currentTenant.TenantId : null) || (_currentTenant == null || _currentTenant.TenantId == null));
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTenant?.TenantId != null)
        {
            foreach (var entry in ChangeTracker.Entries<IMultiTenant>())
            {
                if (entry.State == EntityState.Added && entry.Entity.TenantId == null)
                {
                    entry.Entity.TenantId = _currentTenant.TenantId;
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
