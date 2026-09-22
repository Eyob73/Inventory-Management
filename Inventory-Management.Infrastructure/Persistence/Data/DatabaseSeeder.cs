using Bogus;
using Inventory_Management.Domain.Entities;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Identity;

namespace Inventory_Management.Infrastructure.Persistence.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        // Ensure database is updated to latest migration
        await context.Database.MigrateAsync();

        // ─── 1. Seed Tenants ───────────────────────────────────────────────────
        var acmeTenant = new Tenant
        {
            Id = new Guid("11111111-1111-1111-1111-111111111111"),
            Name = "Acme Corporation",
            Code = "acme",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var techHubTenant = new Tenant
        {
            Id = new Guid("22222222-2222-2222-2222-222222222222"),
            Name = "TechHub Retail",
            Code = "techhub",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        if (!await context.Tenants.AnyAsync())
        {
            await context.Tenants.AddRangeAsync(acmeTenant, techHubTenant);
            await context.SaveChangesAsync();
        }
        else
        {
            // Retrieve existing tenants if already seeded
            acmeTenant = await context.Tenants.FindAsync(acmeTenant.Id) ?? acmeTenant;
            techHubTenant = await context.Tenants.FindAsync(techHubTenant.Id) ?? techHubTenant;
        }

        // ─── 2. Seed Data using Bogus ───────────────
        if (!await context.Categories.IgnoreQueryFilters().AnyAsync())
        {
            Randomizer.Seed = new Random(42);

            var tenants = new[] { acmeTenant, techHubTenant };

            foreach (var tenant in tenants)
            {
                // -- Generate Categories --
                var categoryFaker = new Faker<Category>()
                    .RuleFor(c => c.Id, f => Guid.NewGuid())
                    .RuleFor(c => c.TenantId, f => tenant.Id)
                    .RuleFor(c => c.Name, f => f.Commerce.Categories(1)[0] + " " + f.UniqueIndex)
                    .RuleFor(c => c.Description, f => f.Commerce.ProductDescription());

                var categories = categoryFaker.Generate(10).GroupBy(c => c.Name).Select(g => g.First()).ToList(); // Unique names
                await context.Categories.AddRangeAsync(categories);

                // -- Generate Suppliers --
                var supplierFaker = new Faker<Supplier>()
                    .RuleFor(s => s.Id, f => Guid.NewGuid())
                    .RuleFor(s => s.TenantId, f => tenant.Id)
                    .RuleFor(s => s.Name, f => f.Company.CompanyName())
                    .RuleFor(s => s.ContactName, f => f.Name.FullName())
                    .RuleFor(s => s.Email, f => f.Internet.Email() + f.UniqueIndex) // Ensure unique
                    .RuleFor(s => s.PhoneNumber, f => f.Phone.PhoneNumber("###-###-####")) // Format to stay under 20
                    .RuleFor(s => s.Address, f => { var addr = f.Address.FullAddress(); return addr.Length <= 300 ? addr : addr.Substring(0, 300); });

                var suppliers = supplierFaker.Generate(20);
                await context.Suppliers.AddRangeAsync(suppliers);

                // -- Generate Products --
                var productFaker = new Faker<Product>()
                    .RuleFor(p => p.Id, f => Guid.NewGuid())
                    .RuleFor(p => p.TenantId, f => tenant.Id)
                    .RuleFor(p => p.Name, f => f.Commerce.ProductName())
                    .RuleFor(p => p.SKU, f => f.Commerce.Ean13() + "-" + f.UniqueIndex) // Ensure unique
                    .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
                    .RuleFor(p => p.Cost, f => decimal.Parse(f.Commerce.Price(1, 100)))
                    .RuleFor(p => p.Price, (f, p) => p.Cost * f.Random.Decimal(1.2m, 2.0m)) // Profit margin
                    .RuleFor(p => p.QuantityInStock, f => f.Random.Int(0, 500))
                    .RuleFor(p => p.CategoryId, f => f.PickRandom(categories).Id)
                    .RuleFor(p => p.SupplierId, f => f.PickRandom(suppliers).Id);

                var products = productFaker.Generate(100);
                await context.Products.AddRangeAsync(products);

                // -- Generate Customers --
                var customerFaker = new Faker<Customer>()
                    .RuleFor(c => c.Id, f => Guid.NewGuid())
                    .RuleFor(c => c.TenantId, f => tenant.Id)
                    .RuleFor(c => c.Name, f => f.Company.CompanyName())
                    .RuleFor(c => c.Email, f => f.Internet.Email() + f.UniqueIndex) // Ensure unique
                    .RuleFor(c => c.PhoneNumber, f => f.Phone.PhoneNumber("###-###-####")) // Format to stay under 20
                    .RuleFor(c => c.Address, f => { var addr = f.Address.FullAddress(); return addr.Length <= 300 ? addr : addr.Substring(0, 300); });

                var customers = customerFaker.Generate(50);
                await context.Customers.AddRangeAsync(customers);
            }
        }

        await context.SaveChangesAsync();

        // ─── 3. Seed Roles and Users ──────────────────────────────────────────
        string[] roles = { "SystemAdmin", "Admin", "Manager", "Sales" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Helper function to create users
        async Task CreateUserAsync(string email, string firstName, string lastName, string role, Guid? tenantId)
        {
            if (await userManager.FindByEmailAsync(email) == null)
            {
                var user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    TenantId = tenantId,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, "Password123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
        }

        // 1. System Admin (No TenantId)
        await CreateUserAsync("systemadmin@ims.com", "System", "Admin", "SystemAdmin", null);

        // 2. Company Admins
        await CreateUserAsync("admin@acme.com", "Acme", "Admin", "Admin", acmeTenant.Id);
        await CreateUserAsync("admin@techhub.com", "TechHub", "Admin", "Admin", techHubTenant.Id);

        // 3. Company Managers
        await CreateUserAsync("manager@acme.com", "Acme", "Manager", "Manager", acmeTenant.Id);
        await CreateUserAsync("manager@techhub.com", "TechHub", "Manager", "Manager", techHubTenant.Id);

        // 4. Company Sales
        await CreateUserAsync("sales@acme.com", "Acme", "Sales", "Sales", acmeTenant.Id);
        await CreateUserAsync("sales@techhub.com", "TechHub", "Sales", "Sales", techHubTenant.Id);
    }
}
