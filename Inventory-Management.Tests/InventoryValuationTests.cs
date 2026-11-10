using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Application.DTOs.Common;
using Inventory_Management.Application.DTOs.Report;
using Inventory_Management.Application.Features.Products.Queries;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Application.Services;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;
using Inventory_Management.Infrastructure.Persistence.Data;
using Inventory_Management.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Inventory_Management.Tests;

public class InventoryValuationTests
{
    private sealed class TestCurrentTenant : ICurrentTenant
    {
        public Guid? TenantId { get; private set; }
        public void SetTenant(Guid tenantId) => TenantId = tenantId;
    }

    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"InventoryValuation_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options, new TestCurrentTenant());
    }

    private static ReportService CreateReportService(AppDbContext context)
    {
        var userServiceMock = new Mock<IUserService>();
        userServiceMock
            .Setup(u => u.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Inventory_Management.Application.DTOs.User.UserDto>());

        return new ReportService(
            new GenericRepository<Sale>(context),
            new GenericRepository<SaleItem>(context),
            new GenericRepository<Purchase>(context),
            new GenericRepository<PurchaseItem>(context),
            new GenericRepository<Product>(context),
            new GenericRepository<Customer>(context),
            new GenericRepository<Supplier>(context),
            new GenericRepository<InventoryTransaction>(context),
            userServiceMock.Object);
    }

    [Fact]
    public async Task PagedProductsAndInventoryReport_MatchWeightedPurchaseCostTimesStock_AndHideCostForSales()
    {
        await using var context = CreateInMemoryContext();

        var cat = new Category { Id = Guid.NewGuid(), Name = "Beverages" };
        context.Categories.Add(cat);

        // Product A: Catalog cost = 10, Weighted avg completed purchase cost = (10*20 + 20*35) / 30 = 30.00
        // Stock = 5, Selling Price = 50 -> Cost value = 30 * 5 = 150.00, Retail value = 50 * 5 = 250.00
        var prodA = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Alpha Juice",
            SKU = "SKU-A",
            Barcode = "111111",
            Price = 50m,
            Cost = 10m,
            QuantityInStock = 5,
            MinimumStock = 10,
            CategoryId = cat.Id
        };

        // Product B: No completed purchases -> falls back to Product.Cost = 12.55
        // Stock = 3, Selling Price = 20 -> Cost value = 12.55 * 3 = 37.65, Retail value = 20 * 3 = 60.00
        var prodB = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Beta Water",
            SKU = "SKU-B",
            Barcode = "222222",
            Price = 20m,
            Cost = 12.55m,
            QuantityInStock = 3,
            MinimumStock = 2,
            CategoryId = cat.Id
        };

        // Product C: Fractional weighted average cost from completed purchases = 100 / 3 = 33.3333...
        // Stock = 7 -> Row InventoryValue = Math.Round(7 * (100m / 3m), 2) = 233.33
        // Selling Price = 45.10 -> Retail value = 7 * 45.10 = 315.70
        var prodC = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Gamma Soda",
            SKU = "SKU-C",
            Barcode = "333333",
            Price = 45.10m,
            Cost = 15m,
            QuantityInStock = 7,
            MinimumStock = 2,
            CategoryId = cat.Id
        };

        var completedPurchase = new Purchase
        {
            Id = Guid.NewGuid(),
            PurchaseNumber = "PO-001",
            Status = PurchaseStatus.Completed,
            PurchaseDate = DateTime.UtcNow.AddDays(-2),
            TotalAmount = 1000m
        };
        var draftPurchase = new Purchase
        {
            Id = Guid.NewGuid(),
            PurchaseNumber = "PO-002",
            Status = PurchaseStatus.Draft,
            PurchaseDate = DateTime.UtcNow.AddDays(-1),
            TotalAmount = 9999m
        };

        context.Products.AddRange(prodA, prodB, prodC);
        context.Purchases.AddRange(completedPurchase, draftPurchase);
        context.PurchaseItems.AddRange(
            new PurchaseItem { Id = Guid.NewGuid(), PurchaseId = completedPurchase.Id, ProductId = prodA.Id, Quantity = 10, UnitCost = 20m, TotalCost = 200m },
            new PurchaseItem { Id = Guid.NewGuid(), PurchaseId = completedPurchase.Id, ProductId = prodA.Id, Quantity = 20, UnitCost = 35m, TotalCost = 700m },
            // Draft purchase must be ignored
            new PurchaseItem { Id = Guid.NewGuid(), PurchaseId = draftPurchase.Id, ProductId = prodB.Id, Quantity = 10, UnitCost = 999m, TotalCost = 9990m },
            // Fractional cost purchase for prodC: 100 / 3
            new PurchaseItem { Id = Guid.NewGuid(), PurchaseId = completedPurchase.Id, ProductId = prodC.Id, Quantity = 3, UnitCost = 33.33m, TotalCost = 100m }
        );
        await context.SaveChangesAsync();

        var productRepo = new GenericRepository<Product>(context);
        var purchaseItemRepo = new GenericRepository<PurchaseItem>(context);
        var pagedHandler = new GetPagedProductsQueryHandler(productRepo, purchaseItemRepo);
        var reportService = CreateReportService(context);

        var expectedCostTotal = 150.00m + 37.65m + 233.33m; // 420.98
        var expectedRetailTotal = 250.00m + 60.00m + 315.70m; // 625.70

        // Admin/Manager view on Products page (paged to 1 item to verify whole-catalog KPI)
        var adminPaged = await pagedHandler.Handle(
            new GetPagedProductsQuery(new PagedRequest { Page = 1, PageSize = 1 }, IncludeCost: true),
            CancellationToken.None);

        Assert.Equal(expectedCostTotal, adminPaged.TotalInventoryValue);
        Assert.Equal(prodA.Cost, adminPaged.Items.Single().Cost);

        // Sales view on Products page: retail value shown & Cost masked
        var salesPaged = await pagedHandler.Handle(
            new GetPagedProductsQuery(new PagedRequest { Page = 1, PageSize = 10 }, IncludeCost: false),
            CancellationToken.None);

        Assert.Equal(expectedRetailTotal, salesPaged.TotalInventoryValue);
        Assert.All(salesPaged.Items, item => Assert.Equal(0m, item.Cost));

        // ReportService Inventory Report matches Admin Products page to the cent
        var inventoryReport = await reportService.GetInventoryAsync(new ReportFilterDto { Page = 1, PageSize = 2 });
        Assert.Equal(expectedCostTotal, inventoryReport.Summary.InventoryValue);
        Assert.Equal(expectedRetailTotal, inventoryReport.Summary.InventorySellingValue);

        // Unpaged inventory export matches row-by-row sum
        var fullInventoryReport = await reportService.GetInventoryAsync(new ReportFilterDto { Page = 1, PageSize = 100 });
        Assert.Equal(fullInventoryReport.Table.Items.Sum(r => r.InventoryValue), inventoryReport.Summary.InventoryValue);
    }

    [Fact]
    public async Task SingleAndAllProductQueries_MaskCostWhenIncludeCostIsFalse()
    {
        await using var context = CreateInMemoryContext();
        var cat = new Category { Id = Guid.NewGuid(), Name = "General" };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Mask Test",
            SKU = "SKU-MASK",
            Barcode = "987654321",
            Price = 120m,
            Cost = 75m,
            QuantityInStock = 10,
            CategoryId = cat.Id
        };
        context.Categories.Add(cat);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var repo = new GenericRepository<Product>(context);

        var allSales = (await new GetAllProductsQueryHandler(repo).Handle(new GetAllProductsQuery(IncludeCost: false), CancellationToken.None)).ToList();
        var allAdmin = (await new GetAllProductsQueryHandler(repo).Handle(new GetAllProductsQuery(IncludeCost: true), CancellationToken.None)).ToList();
        Assert.Equal(0m, allSales.Single().Cost);
        Assert.Equal(75m, allAdmin.Single().Cost);

        var byIdSales = await new GetProductByIdQueryHandler(repo).Handle(new GetProductByIdQuery(product.Id, IncludeCost: false), CancellationToken.None);
        var byIdAdmin = await new GetProductByIdQueryHandler(repo).Handle(new GetProductByIdQuery(product.Id, IncludeCost: true), CancellationToken.None);
        Assert.Equal(0m, byIdSales.Cost);
        Assert.Equal(75m, byIdAdmin.Cost);

        var byBarcodeSales = await new GetProductByBarcodeQueryHandler(repo).Handle(new GetProductByBarcodeQuery("987654321", IncludeCost: false), CancellationToken.None);
        var byBarcodeAdmin = await new GetProductByBarcodeQueryHandler(repo).Handle(new GetProductByBarcodeQuery("987654321", IncludeCost: true), CancellationToken.None);
        Assert.Equal(0m, byBarcodeSales.Cost);
        Assert.Equal(75m, byBarcodeAdmin.Cost);
    }

    [Fact]
    public async Task Exports_InventoryPdfAndCompositeReportWithOver100Products_GenerateValidOutputAndAccurateTotals()
    {
        await using var context = CreateInMemoryContext();
        var cat = new Category { Id = Guid.NewGuid(), Name = "Bulk Items" };
        context.Categories.Add(cat);

        // Create 105 products to exercise Composite Report pagination cap (100 rows in table, 105 in summary)
        // plus Critical Stock, Low Stock, and Out of Stock items
        for (int i = 1; i <= 105; i++)
        {
            var stock = i switch
            {
                1 => 0,  // Out of Stock
                2 => 2,  // Critical Stock (Min = 10, <= 5)
                3 => 8,  // Low Stock (Min = 10, <= 10)
                _ => 10  // In Stock
            };

            context.Products.Add(new Product
            {
                Id = Guid.NewGuid(),
                Name = $"Product {i:000}",
                SKU = $"SKU-{i:000}",
                Price = 20m,
                Cost = 10m,
                QuantityInStock = stock,
                MinimumStock = 10,
                CategoryId = cat.Id
            });
        }
        await context.SaveChangesAsync();

        var reportService = CreateReportService(context);
        var filter = new ReportFilterDto();

        var expectedTotalStock = 0 + 2 + 8 + (102 * 10); // 1030
        var expectedCostValue = expectedTotalStock * 10m; // 10300.00
        var expectedSellingValue = expectedTotalStock * 20m; // 20600.00

        // CSV export verifies exact metadata, Tenant/User claims, and Unit Cost column
        var csvExport = await reportService.ExportAsync("inventory", filter, "csv", "Acme Trading", "Manager Jane");
        var csvText = Encoding.UTF8.GetString(csvExport.Content);
        Assert.Contains("Tenant,Acme Trading", csvText);
        Assert.Contains("User,Manager Jane", csvText);
        Assert.Contains("Unit Cost", csvText);
        Assert.Contains("Total Products,105", csvText);
        Assert.Contains($"Total Stock Quantity,{expectedTotalStock}", csvText);
        Assert.Contains($"Total Inventory Value,\"ETB {expectedCostValue:N2}\"", csvText);
        Assert.Contains($"Inventory Selling Value,\"ETB {expectedSellingValue:N2}\"", csvText);

        // Single Inventory PDF & Low Stock PDF render cleanly (10 columns -> landscape)
        var inventoryPdf = await reportService.ExportAsync("inventory", filter, "pdf", "Acme Trading", "Manager Jane");
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(inventoryPdf.Content, 0, 5));

        var lowStockPdf = await reportService.ExportAsync("low-stock", filter, "pdf", "Acme Trading", "Manager Jane");
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(lowStockPdf.Content, 0, 5));

        // Composite PDF with 105 items (including Critical Stock) renders cleanly
        var compositePdf = await reportService.ExportCompositeAsync("Acme Trading", "Manager Jane", filter, "pdf");
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(compositePdf.Content, 0, 5));

        // Composite Excel contains Inventory Value, Inventory Selling Value, Unit Cost
        var compositeXls = await reportService.ExportCompositeAsync("Acme Trading", "Manager Jane", filter, "xlsx");
        var xlsText = Encoding.UTF8.GetString(compositeXls.Content);
        Assert.Contains($"ETB {expectedCostValue:N2}", xlsText);
        Assert.Contains($"ETB {expectedSellingValue:N2}", xlsText);
        Assert.Contains("Unit Cost", xlsText);
    }
}

