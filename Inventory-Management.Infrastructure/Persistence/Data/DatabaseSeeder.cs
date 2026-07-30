using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inventory_Management.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Infrastructure.Persistence.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Ensure database is updated to latest migration
        await context.Database.MigrateAsync();

        // 1. Seed Roles & Users
        if (!await context.Roles.AnyAsync())
        {
            var adminRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = "Admin",
                Description = "Administrator with full system permissions"
            };
            var managerRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = "Manager",
                Description = "Inventory Manager"
            };
            var staffRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = "Cashier",
                Description = "Cashier"
            };

            await context.Roles.AddRangeAsync(adminRole, managerRole, staffRole);

            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@inventory.com",
                FirstName = "System",
                LastName = "Admin",
                PasswordHash = "AQAAAAEAACcQAAAAEHx",
                IsActive = true,
                RoleId = adminRole.Id
            };

            var managerUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "manager",
                Email = "manager@inventory.com",
                FirstName = "Jane",
                LastName = "Manager",
                PasswordHash = "AQAAAAEAACcQAAAAEHx",
                IsActive = true,
                RoleId = managerRole.Id
            };

            await context.Users.AddRangeAsync(adminUser, managerUser);
        }

        // 2. Seed Categories, Suppliers, Products, and Customers
        if (!await context.Categories.AnyAsync())
        {
            var electronics = new Category
            {
                Id = Guid.NewGuid(),
                Name = "Electronics",
                Description = "Gadgets and electronic hardware"
            };
            var furniture = new Category
            {
                Id = Guid.NewGuid(),
                Name = "Furniture",
                Description = "Office and home furniture"
            };
            var stationery = new Category
            {
                Id = Guid.NewGuid(),
                Name = "Stationery",
                Description = "Office supplies and paper products"
            };

            await context.Categories.AddRangeAsync(electronics, furniture, stationery);

            var supplierTech = new Supplier
            {
                Id = Guid.NewGuid(),
                Name = "TechSupply Co.",
                ContactName = "Robert Paulson",
                Email = "contact@techsupply.com",
                PhoneNumber = "+1-555-0192",
                Address = "100 Tech Blvd, Silicon Valley, CA"
            };
            var supplierFurniture = new Supplier
            {
                Id = Guid.NewGuid(),
                Name = "Global Office Depot",
                ContactName = "Sarah Jenkins",
                Email = "sales@globalofficedepot.com",
                PhoneNumber = "+1-555-0143",
                Address = "450 Industrial Parkway, Chicago, IL"
            };

            await context.Suppliers.AddRangeAsync(supplierTech, supplierFurniture);

            var products = new List<Product>
            {
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Pro Wireless Mouse",
                    SKU = "ELEC-WMO-001",
                    Description = "Ergonomic 2.4GHz Wireless Mouse",
                    Price = 29.99m,
                    Cost = 14.50m,
                    QuantityInStock = 150,
                    CategoryId = electronics.Id,
                    SupplierId = supplierTech.Id
                },
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Mechanical Keyboard",
                    SKU = "ELEC-MKB-002",
                    Description = "RGB Backlit Mechanical Gaming Keyboard",
                    Price = 89.99m,
                    Cost = 45.00m,
                    QuantityInStock = 75,
                    CategoryId = electronics.Id,
                    SupplierId = supplierTech.Id
                },
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Ergonomic Office Chair",
                    SKU = "FURN-EOC-001",
                    Description = "High-back mesh chair with lumbar support",
                    Price = 249.99m,
                    Cost = 130.00m,
                    QuantityInStock = 30,
                    CategoryId = furniture.Id,
                    SupplierId = supplierFurniture.Id
                },
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Adjustable Standing Desk",
                    SKU = "FURN-ASD-002",
                    Description = "Electric dual-motor standing desk 55 inch",
                    Price = 499.99m,
                    Cost = 280.00m,
                    QuantityInStock = 15,
                    CategoryId = furniture.Id,
                    SupplierId = supplierFurniture.Id
                }
            };

            await context.Products.AddRangeAsync(products);

            var customer1 = new Customer
            {
                Id = Guid.NewGuid(),
                Name = "Acme Corp",
                Email = "purchasing@acmecorp.com",
                PhoneNumber = "+1-555-0800",
                Address = "789 Corporate Way, New York, NY"
            };
            var customer2 = new Customer
            {
                Id = Guid.NewGuid(),
                Name = "John Doe",
                Email = "johndoe@example.com",
                PhoneNumber = "+1-555-0911",
                Address = "123 Main Street, Austin, TX"
            };

            await context.Customers.AddRangeAsync(customer1, customer2);
        }

        await context.SaveChangesAsync();
    }
}
