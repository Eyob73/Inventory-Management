using Inventory_Management.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Infrastructure.Persistence.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
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

        // ─── 2. Seed Roles & Users ─────────────────────────────────────────────
        if (!await context.Roles.IgnoreQueryFilters().AnyAsync())
        {
            var adminRoleAcme = new Role
            {
                Id = Guid.NewGuid(),
                TenantId = acmeTenant.Id,
                Name = "Admin",
                Description = "Administrator with full system permissions"
            };
            var managerRoleAcme = new Role
            {
                Id = Guid.NewGuid(),
                TenantId = acmeTenant.Id,
                Name = "Manager",
                Description = "Inventory Manager"
            };
            var cashierRoleAcme = new Role
            {
                Id = Guid.NewGuid(),
                TenantId = acmeTenant.Id,
                Name = "Cashier",
                Description = "Point-of-Sale Cashier"
            };
            var adminRoleTechHub = new Role
            {
                Id = Guid.NewGuid(),
                TenantId = techHubTenant.Id,
                Name = "Admin",
                Description = "Administrator with full system permissions"
            };
            var managerRoleTechHub = new Role
            {
                Id = Guid.NewGuid(),
                TenantId = techHubTenant.Id,
                Name = "Manager",
                Description = "Inventory Manager"
            };

            await context.Roles.AddRangeAsync(
                adminRoleAcme, managerRoleAcme, cashierRoleAcme,
                adminRoleTechHub, managerRoleTechHub
            );

            await context.Users.AddRangeAsync(
                new User
                {
                    Id = Guid.NewGuid(),
                    TenantId = acmeTenant.Id,
                    Username = "acme.admin",
                    Email = "admin@acme.com",
                    FirstName = "Alice",
                    LastName = "Admin",
                    PasswordHash = "AQAAAAEAACcQAAAAEHx",
                    IsActive = true,
                    RoleId = adminRoleAcme.Id
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    TenantId = acmeTenant.Id,
                    Username = "acme.manager",
                    Email = "manager@acme.com",
                    FirstName = "Bob",
                    LastName = "Manager",
                    PasswordHash = "AQAAAAEAACcQAAAAEHx",
                    IsActive = true,
                    RoleId = managerRoleAcme.Id
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    TenantId = techHubTenant.Id,
                    Username = "techhub.admin",
                    Email = "admin@techhub.com",
                    FirstName = "Carol",
                    LastName = "Admin",
                    PasswordHash = "AQAAAAEAACcQAAAAEHx",
                    IsActive = true,
                    RoleId = adminRoleTechHub.Id
                }
            );
        }

        // ─── 3. Seed Categories, Suppliers, Products, Customers ───────────────
        if (!await context.Categories.IgnoreQueryFilters().AnyAsync())
        {
            // ── Acme's Categories
            var electronics = new Category { Id = Guid.NewGuid(), TenantId = acmeTenant.Id, Name = "Electronics", Description = "Gadgets and electronic hardware" };
            var furniture   = new Category { Id = Guid.NewGuid(), TenantId = acmeTenant.Id, Name = "Furniture",   Description = "Office and home furniture" };
            var stationery  = new Category { Id = Guid.NewGuid(), TenantId = acmeTenant.Id, Name = "Stationery",  Description = "Office supplies and paper products" };

            // ── TechHub's Categories
            var networking  = new Category { Id = Guid.NewGuid(), TenantId = techHubTenant.Id, Name = "Networking",  Description = "Networking hardware and accessories" };
            var peripherals = new Category { Id = Guid.NewGuid(), TenantId = techHubTenant.Id, Name = "Peripherals", Description = "Computer peripherals" };

            await context.Categories.AddRangeAsync(electronics, furniture, stationery, networking, peripherals);

            // ── Acme's Suppliers
            var supplierTech = new Supplier
            {
                Id = Guid.NewGuid(), TenantId = acmeTenant.Id,
                Name = "TechSupply Co.", ContactName = "Robert Paulson",
                Email = "contact@techsupply.com", PhoneNumber = "+1-555-0192",
                Address = "100 Tech Blvd, Silicon Valley, CA"
            };
            var supplierFurniture = new Supplier
            {
                Id = Guid.NewGuid(), TenantId = acmeTenant.Id,
                Name = "Global Office Depot", ContactName = "Sarah Jenkins",
                Email = "sales@globalofficedepot.com", PhoneNumber = "+1-555-0143",
                Address = "450 Industrial Parkway, Chicago, IL"
            };

            // ── TechHub's Suppliers
            var supplierNetwork = new Supplier
            {
                Id = Guid.NewGuid(), TenantId = techHubTenant.Id,
                Name = "NetGear Distributors", ContactName = "Mark Chen",
                Email = "orders@netgeardist.com", PhoneNumber = "+1-555-0377",
                Address = "200 Data Drive, Austin, TX"
            };

            await context.Suppliers.AddRangeAsync(supplierTech, supplierFurniture, supplierNetwork);

            // ── Acme's Products
            await context.Products.AddRangeAsync(
                new Product { Id = Guid.NewGuid(), TenantId = acmeTenant.Id, Name = "Pro Wireless Mouse",   SKU = "ELEC-WMO-001", Description = "Ergonomic 2.4GHz Wireless Mouse",             Price = 29.99m,  Cost = 14.50m,  QuantityInStock = 150, CategoryId = electronics.Id, SupplierId = supplierTech.Id },
                new Product { Id = Guid.NewGuid(), TenantId = acmeTenant.Id, Name = "Mechanical Keyboard",  SKU = "ELEC-MKB-002", Description = "RGB Backlit Mechanical Gaming Keyboard",        Price = 89.99m,  Cost = 45.00m,  QuantityInStock = 75,  CategoryId = electronics.Id, SupplierId = supplierTech.Id },
                new Product { Id = Guid.NewGuid(), TenantId = acmeTenant.Id, Name = "Ergonomic Chair",      SKU = "FURN-EOC-001", Description = "High-back mesh chair with lumbar support",      Price = 249.99m, Cost = 130.00m, QuantityInStock = 30,  CategoryId = furniture.Id,   SupplierId = supplierFurniture.Id },
                new Product { Id = Guid.NewGuid(), TenantId = acmeTenant.Id, Name = "Standing Desk",        SKU = "FURN-ASD-002", Description = "Electric dual-motor standing desk 55 inch",     Price = 499.99m, Cost = 280.00m, QuantityInStock = 15,  CategoryId = furniture.Id,   SupplierId = supplierFurniture.Id },
                new Product { Id = Guid.NewGuid(), TenantId = acmeTenant.Id, Name = "A4 Printing Paper",    SKU = "STAT-PPR-001", Description = "500-sheet ream of 80gsm A4 printing paper",     Price = 8.99m,   Cost = 4.00m,   QuantityInStock = 500, CategoryId = stationery.Id,  SupplierId = supplierFurniture.Id }
            );

            // ── TechHub's Products
            await context.Products.AddRangeAsync(
                new Product { Id = Guid.NewGuid(), TenantId = techHubTenant.Id, Name = "24-Port Managed Switch", SKU = "NET-SW-001",  Description = "Gigabit 24-port managed network switch", Price = 349.99m, Cost = 180.00m, QuantityInStock = 20, CategoryId = networking.Id,  SupplierId = supplierNetwork.Id },
                new Product { Id = Guid.NewGuid(), TenantId = techHubTenant.Id, Name = "Dual-Band Wi-Fi Router",  SKU = "NET-WR-002",  Description = "AX3000 dual-band Wi-Fi 6 router",       Price = 199.99m, Cost = 95.00m,  QuantityInStock = 40, CategoryId = networking.Id,  SupplierId = supplierNetwork.Id },
                new Product { Id = Guid.NewGuid(), TenantId = techHubTenant.Id, Name = "USB-C Hub 7-in-1",        SKU = "PERI-USB-001", Description = "7-in-1 USB-C hub with HDMI and PD",    Price = 49.99m,  Cost = 22.00m,  QuantityInStock = 90, CategoryId = peripherals.Id, SupplierId = supplierNetwork.Id }
            );

            // ── Acme's Customers
            await context.Customers.AddRangeAsync(
                new Customer { Id = Guid.NewGuid(), TenantId = acmeTenant.Id, Name = "Metropolis Inc.",  Email = "orders@metropolis.com",  PhoneNumber = "+1-555-0800", Address = "789 Corporate Way, New York, NY" },
                new Customer { Id = Guid.NewGuid(), TenantId = acmeTenant.Id, Name = "John Doe",         Email = "johndoe@example.com",    PhoneNumber = "+1-555-0911", Address = "123 Main Street, Austin, TX" },
                new Customer { Id = Guid.NewGuid(), TenantId = acmeTenant.Id, Name = "Stark Enterprises", Email = "supply@stark.com",       PhoneNumber = "+1-555-0999", Address = "10880 Malibu Point, CA" }
            );

            // ── TechHub's Customers
            await context.Customers.AddRangeAsync(
                new Customer { Id = Guid.NewGuid(), TenantId = techHubTenant.Id, Name = "CloudBase Systems", Email = "info@cloudbase.io",      PhoneNumber = "+1-555-0451", Address = "77 Cloud Ave, Seattle, WA" },
                new Customer { Id = Guid.NewGuid(), TenantId = techHubTenant.Id, Name = "DataEdge Ltd.",      Email = "purchasing@dataedge.com", PhoneNumber = "+1-555-0762", Address = "300 Data Lane, San Jose, CA" }
            );
        }

        await context.SaveChangesAsync();
    }
}
