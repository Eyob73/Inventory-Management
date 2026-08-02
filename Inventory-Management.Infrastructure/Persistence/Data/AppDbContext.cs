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

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            bool isMultiTenant = typeof(IMultiTenant).IsAssignableFrom(clrType);
            bool isSoftDelete  = typeof(ISoftDelete).IsAssignableFrom(clrType);

            if (isMultiTenant && isSoftDelete)
            {
                // Combined filter: tenant isolation + soft-delete exclusion
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(SetCombinedQueryFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(clrType);
                method.Invoke(this, [modelBuilder]);
            }
            else if (isMultiTenant)
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(SetTenantQueryFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(clrType);
                method.Invoke(this, [modelBuilder]);
            }
            else if (isSoftDelete)
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(SetSoftDeleteQueryFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(clrType);
                method.Invoke(this, [modelBuilder]);
            }
        }
    }

    private void SetTenantQueryFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IMultiTenant
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            e.TenantId == (_currentTenant != null ? _currentTenant.TenantId : null)
            || _currentTenant == null
            || _currentTenant.TenantId == null);
    }

    private void SetCombinedQueryFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IMultiTenant, ISoftDelete
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            !e.IsDeleted
            && (e.TenantId == (_currentTenant != null ? _currentTenant.TenantId : null)
                || _currentTenant == null
                || _currentTenant.TenantId == null));
    }

    private void SetSoftDeleteQueryFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ISoftDelete
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            // Auto-assign TenantId on new multi-tenant entities
            if (entry.State == EntityState.Added
                && entry.Entity is IMultiTenant multiTenantEntity
                && multiTenantEntity.TenantId == null
                && _currentTenant?.TenantId != null)
            {
                multiTenantEntity.TenantId = _currentTenant.TenantId;
            }

            // Convert hard-delete to soft-delete for ISoftDelete entities
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDelete softDeleteEntity)
            {
                entry.State = EntityState.Modified;
                softDeleteEntity.IsDeleted = true;
                softDeleteEntity.DeletedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
