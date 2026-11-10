using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Services;

/// <summary>
/// Centralizes weighted purchase cost lookup and inventory valuation math so that
/// Products catalog KPIs, Inventory Reports, Profit/COGS calculations, and PDF exports
/// produce consistent figures.
/// </summary>
public static class InventoryValuationHelper
{
    /// <summary>
    /// Computes the weighted average unit cost from completed purchase items per product.
    /// Products with no completed purchases are omitted so callers fall back to <see cref="Product.Cost"/>.
    /// </summary>
    public static async Task<Dictionary<Guid, decimal>> GetPurchaseUnitCostsAsync(
        IGenericRepository<PurchaseItem>? purchaseItems,
        CancellationToken cancellationToken = default)
    {
        if (purchaseItems == null)
            return new Dictionary<Guid, decimal>();

        var fromPurchases = await purchaseItems.Query().AsNoTracking()
            .Where(pi => pi.Purchase != null && pi.Purchase.Status == PurchaseStatus.Completed && pi.Quantity > 0)
            .GroupBy(pi => pi.ProductId)
            .Select(g => new { ProductId = g.Key, Cost = g.Sum(x => x.TotalCost) / g.Sum(x => x.Quantity) })
            .ToListAsync(cancellationToken);

        return fromPurchases.ToDictionary(x => x.ProductId, x => x.Cost);
    }

    /// <summary>
    /// Resolves unit cost for every product in the current tenant:
    /// weighted average of completed purchase costs, falling back to <see cref="Product.Cost"/>.
    /// </summary>
    public static async Task<Dictionary<Guid, decimal>> GetUnitCostsAsync(
        IGenericRepository<Product> products,
        IGenericRepository<PurchaseItem>? purchaseItems,
        CancellationToken cancellationToken = default)
    {
        var productCosts = await products.Query().AsNoTracking()
            .Select(p => new { p.Id, p.Cost })
            .ToListAsync(cancellationToken);

        var purchaseUnitCosts = await GetPurchaseUnitCostsAsync(purchaseItems, cancellationToken);

        var dict = productCosts.ToDictionary(p => p.Id, p => p.Cost);
        foreach (var row in purchaseUnitCosts)
            dict[row.Key] = row.Value;

        return dict;
    }

    public static decimal CalculateRowInventoryValue(int quantityInStock, decimal unitCost)
        => Math.Round(quantityInStock * unitCost, 2);

    public static decimal CalculateCostInventoryValue(
        IEnumerable<(Guid ProductId, decimal CatalogCost, int QuantityInStock)> products,
        IReadOnlyDictionary<Guid, decimal> purchaseUnitCosts)
    {
        decimal total = 0m;
        foreach (var p in products)
        {
            var unitCost = purchaseUnitCosts.GetValueOrDefault(p.ProductId, p.CatalogCost);
            total += CalculateRowInventoryValue(p.QuantityInStock, unitCost);
        }
        return Math.Round(total, 2);
    }

    public static decimal CalculateSellingInventoryValue(
        IEnumerable<(int QuantityInStock, decimal SellingPrice)> products)
    {
        decimal total = 0m;
        foreach (var p in products)
        {
            total += CalculateRowInventoryValue(p.QuantityInStock, p.SellingPrice);
        }
        return Math.Round(total, 2);
    }
}

