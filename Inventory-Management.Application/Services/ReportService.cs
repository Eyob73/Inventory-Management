using System.Globalization;
using System.Text;
using Inventory_Management.Application.DTOs.Common;
using Inventory_Management.Application.DTOs.Report;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Services;

public class ReportService : IReportService
{
    private const string CompletedSale = "Completed";
    private const int InsightLimit = 8;
    private const int ExportMaxRows = 5000;

    private readonly IGenericRepository<Sale> _sales;
    private readonly IGenericRepository<SaleItem> _saleItems;
    private readonly IGenericRepository<Purchase> _purchases;
    private readonly IGenericRepository<PurchaseItem> _purchaseItems;
    private readonly IGenericRepository<Product> _products;
    private readonly IGenericRepository<Customer> _customers;
    private readonly IGenericRepository<Supplier> _suppliers;
    private readonly IGenericRepository<InventoryTransaction> _transactions;
    private readonly IUserService _userService;

    public ReportService(
        IGenericRepository<Sale> sales,
        IGenericRepository<SaleItem> saleItems,
        IGenericRepository<Purchase> purchases,
        IGenericRepository<PurchaseItem> purchaseItems,
        IGenericRepository<Product> products,
        IGenericRepository<Customer> customers,
        IGenericRepository<Supplier> suppliers,
        IGenericRepository<InventoryTransaction> transactions,
        IUserService userService)
    {
        _sales = sales;
        _saleItems = saleItems;
        _purchases = purchases;
        _purchaseItems = purchaseItems;
        _products = products;
        _customers = customers;
        _suppliers = suppliers;
        _transactions = transactions;
        _userService = userService;
    }

    public async Task<DashboardReportDto> GetDashboardAsync(ReportFilterDto filter, CancellationToken cancellationToken = default)
    {
        var (start, end) = NormalizeRange(filter);
        var (prevStart, prevEnd) = PreviousRange(start, end);
        var costs = await GetUnitCostsAsync(cancellationToken);

        var currentSales = await SumCompletedSalesAsync(start, end, filter, cancellationToken);
        var previousSales = await SumCompletedSalesAsync(prevStart, prevEnd, filter, cancellationToken);
        var currentPurchases = await SumCompletedPurchasesAsync(start, end, filter, cancellationToken);
        var previousPurchases = await SumCompletedPurchasesAsync(prevStart, prevEnd, filter, cancellationToken);
        var currentQty = await SumSoldQuantityAsync(start, end, filter, cancellationToken);
        var previousQty = await SumSoldQuantityAsync(prevStart, prevEnd, filter, cancellationToken);
        var currentProfit = await SumProfitAsync(start, end, filter, costs, cancellationToken);
        var previousProfit = await SumProfitAsync(prevStart, prevEnd, filter, costs, cancellationToken);

        var customersNow = await _customers.Query().AsNoTracking().CountAsync(cancellationToken);
        var customersPrev = await _customers.Query().AsNoTracking().CountAsync(c => c.CreatedAt < start, cancellationToken);
        var suppliersNow = await _suppliers.Query().AsNoTracking().CountAsync(cancellationToken);
        var suppliersPrev = await _suppliers.Query().AsNoTracking().CountAsync(s => s.CreatedAt < start, cancellationToken);

        var stock = await StockCountsAsync(filter, cancellationToken);
        var dailySales = await DailySalesAsync(start, end, filter, cancellationToken);
        var dailyPurchases = await DailyPurchasesAsync(start, end, filter, cancellationToken);

        return new DashboardReportDto
        {
            StartDate = start,
            EndDate = end,
            TotalSales = Metric(currentSales, previousSales),
            TotalPurchases = Metric(currentPurchases, previousPurchases),
            TotalProfit = Metric(currentProfit, previousProfit),
            TotalProductsSold = Metric(currentQty, previousQty),
            TotalCustomers = Metric(customersNow, customersPrev),
            TotalSuppliers = Metric(suppliersNow, suppliersPrev),
            LowStockProducts = stock.Low,
            OutOfStockProducts = stock.Out,
            SalesOverTime = ToChart(dailySales),
            PurchasesOverTime = ToChart(dailyPurchases),
            TopProducts = await TopSoldProductsAsync(start, end, filter, 6, cancellationToken),
            SalesByCategory = await SalesByCategoryAsync(start, end, filter, cancellationToken)
        };
    }

    public async Task<SalesReportDto> GetSalesAsync(ReportFilterDto filter, CancellationToken cancellationToken = default)
    {
        var (start, end) = NormalizeRange(filter);
        var page = ClampPage(filter);
        var pageSize = ClampPageSize(filter);

        var completed = ApplySaleFilters(CompletedSales(start, end), filter);
        var summary = new SalesReportSummaryDto
        {
            TotalSalesAmount = await completed.SumAsync(s => (decimal?)s.TotalAmount, cancellationToken) ?? 0m,
            NumberOfSales = await completed.CountAsync(cancellationToken),
            TotalProductsSold = await ApplySaleItemFilters(
                    _saleItems.Query().AsNoTracking().Where(i => i.Sale != null && i.Sale.Status == CompletedSale && i.Sale.SaleDate >= start && i.Sale.SaleDate <= end),
                    filter)
                .SumAsync(i => (int?)i.Quantity, cancellationToken) ?? 0,
            TotalDiscounts = await completed.SumAsync(s => (decimal?)s.DiscountAmount, cancellationToken) ?? 0m,
            TotalTax = await completed.SumAsync(s => (decimal?)s.TaxAmount, cancellationToken) ?? 0m
        };
        summary.AverageSaleValue = summary.NumberOfSales == 0
            ? 0m
            : Math.Round(summary.TotalSalesAmount / summary.NumberOfSales, 2);

        var tableQuery = ApplySaleFilters(
            _sales.Query().AsNoTracking()
                .Include(s => s.SaleItems)
                .Where(s => s.SaleDate >= start && s.SaleDate <= end),
            filter);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            tableQuery = tableQuery.Where(s => s.Status == filter.Status);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            tableQuery = tableQuery.Where(s =>
                s.SaleNumber.ToLower().Contains(term)
                || (s.CustomerName != null && s.CustomerName.ToLower().Contains(term))
                || s.PaymentMethod.ToLower().Contains(term));
        }

        tableQuery = SortSales(tableQuery, filter);
        var totalCount = await tableQuery.CountAsync(cancellationToken);
        var rows = await tableQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SalesReportRowDto
            {
                Id = s.Id,
                InvoiceNumber = s.SaleNumber,
                Customer = s.CustomerName ?? "Walk-in",
                Date = s.SaleDate,
                NumberOfItems = s.SaleItems.Sum(i => i.Quantity),
                TotalAmount = s.TotalAmount,
                PaymentMethod = s.PaymentMethod,
                Status = s.Status
            })
            .ToListAsync(cancellationToken);

        var daily = await DailySalesAsync(start, end, filter, cancellationToken);

        return new SalesReportDto
        {
            Summary = summary,
            Table = Page(rows, totalCount, page, pageSize),
            SalesByDay = ToChart(daily),
            SalesByWeek = RollupWeeks(daily),
            SalesByMonth = RollupMonths(daily),
            TopSellingProducts = await TopSoldProductsAsync(start, end, filter, InsightLimit, cancellationToken),
            SalesByCategory = await SalesByCategoryAsync(start, end, filter, cancellationToken)
        };
    }

    public async Task<PurchasesReportDto> GetPurchasesAsync(ReportFilterDto filter, CancellationToken cancellationToken = default)
    {
        var (start, end) = NormalizeRange(filter);
        var page = ClampPage(filter);
        var pageSize = ClampPageSize(filter);

        var completed = ApplyPurchaseFilters(CompletedPurchases(start, end), filter);
        var itemQuery = ApplyPurchaseItemFilters(
            _purchaseItems.Query().AsNoTracking()
                .Where(i => i.Purchase != null
                    && i.Purchase.Status == PurchaseStatus.Completed
                    && i.Purchase.PurchaseDate >= start
                    && i.Purchase.PurchaseDate <= end),
            filter);

        var summary = new PurchasesReportSummaryDto
        {
            TotalPurchaseAmount = await completed.SumAsync(p => (decimal?)p.TotalAmount, cancellationToken) ?? 0m,
            NumberOfPurchases = await completed.CountAsync(cancellationToken),
            TotalProductsPurchased = await itemQuery.SumAsync(i => (int?)i.Quantity, cancellationToken) ?? 0
        };
        summary.AveragePurchaseValue = summary.NumberOfPurchases == 0
            ? 0m
            : Math.Round(summary.TotalPurchaseAmount / summary.NumberOfPurchases, 2);

        var tableQuery = ApplyPurchaseFilters(
            _purchases.Query().AsNoTracking()
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseItems)
                .Where(p => p.PurchaseDate >= start && p.PurchaseDate <= end),
            filter);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            tableQuery = tableQuery.Where(p => p.Status == filter.Status);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            tableQuery = tableQuery.Where(p =>
                p.PurchaseNumber.ToLower().Contains(term)
                || (p.Supplier != null && p.Supplier.Name.ToLower().Contains(term)));
        }

        tableQuery = filter.SortBy?.ToLowerInvariant() switch
        {
            "purchasenumber" => filter.Descending ? tableQuery.OrderByDescending(p => p.PurchaseNumber) : tableQuery.OrderBy(p => p.PurchaseNumber),
            "supplier" => filter.Descending ? tableQuery.OrderByDescending(p => p.Supplier!.Name) : tableQuery.OrderBy(p => p.Supplier!.Name),
            "amount" => filter.Descending ? tableQuery.OrderByDescending(p => p.TotalAmount) : tableQuery.OrderBy(p => p.TotalAmount),
            "status" => filter.Descending ? tableQuery.OrderByDescending(p => p.Status) : tableQuery.OrderBy(p => p.Status),
            _ => filter.Descending ? tableQuery.OrderByDescending(p => p.PurchaseDate) : tableQuery.OrderBy(p => p.PurchaseDate)
        };

        var totalCount = await tableQuery.CountAsync(cancellationToken);
        var rows = await tableQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PurchasesReportRowDto
            {
                Id = p.Id,
                PurchaseNumber = p.PurchaseNumber,
                Supplier = p.Supplier != null ? p.Supplier.Name : "No supplier",
                PurchaseDate = p.PurchaseDate,
                NumberOfItems = p.PurchaseItems.Sum(i => i.Quantity),
                TotalAmount = p.TotalAmount,
                Status = p.Status
            })
            .ToListAsync(cancellationToken);

        var bySupplier = await completed
            .GroupBy(p => new { p.SupplierId, Name = p.Supplier != null ? p.Supplier.Name : "No supplier" })
            .Select(g => new NamedAmountDto
            {
                Id = g.Key.SupplierId ?? Guid.Empty,
                Name = g.Key.Name,
                Amount = g.Sum(x => x.TotalAmount),
                Quantity = g.Count()
            })
            .OrderByDescending(x => x.Amount)
            .Take(InsightLimit)
            .ToListAsync(cancellationToken);

        var mostPurchased = await itemQuery
            .GroupBy(i => new { i.ProductId, Name = i.Product != null ? i.Product.Name : "Unknown" })
            .Select(g => new NamedAmountDto
            {
                Id = g.Key.ProductId,
                Name = g.Key.Name,
                Quantity = g.Sum(x => x.Quantity),
                Amount = g.Sum(x => x.TotalCost)
            })
            .OrderByDescending(x => x.Quantity)
            .Take(InsightLimit)
            .ToListAsync(cancellationToken);

        var byCategory = await itemQuery
            .GroupBy(i => new
            {
                CategoryId = i.Product != null ? i.Product.CategoryId : Guid.Empty,
                Name = i.Product != null && i.Product.Category != null ? i.Product.Category.Name : "Uncategorized"
            })
            .Select(g => new NamedAmountDto
            {
                Id = g.Key.CategoryId,
                Name = g.Key.Name,
                Amount = g.Sum(x => x.TotalCost),
                Quantity = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.Amount)
            .Take(InsightLimit)
            .ToListAsync(cancellationToken);

        return new PurchasesReportDto
        {
            Summary = summary,
            Table = Page(rows, totalCount, page, pageSize),
            PurchasesOverTime = ToChart(await DailyPurchasesAsync(start, end, filter, cancellationToken)),
            PurchasesBySupplier = bySupplier,
            MostPurchasedProducts = mostPurchased,
            PurchaseAmountByCategory = byCategory
        };
    }

    public Task<InventoryReportDto> GetInventoryAsync(ReportFilterDto filter, CancellationToken cancellationToken = default)
        => BuildInventoryAsync(filter, lowStockOnly: false, cancellationToken);

    public async Task<LowStockReportDto> GetLowStockAsync(ReportFilterDto filter, CancellationToken cancellationToken = default)
    {
        var inventory = await BuildInventoryAsync(filter, lowStockOnly: true, cancellationToken);
        return new LowStockReportDto
        {
            OutOfStock = inventory.Summary.OutOfStockProducts,
            CriticalStock = inventory.Summary.CriticalStockProducts,
            LowStock = inventory.Summary.LowStockProducts,
            Table = inventory.Table
        };
    }

    public async Task<ProductPerformanceReportDto> GetProductsAsync(ReportFilterDto filter, CancellationToken cancellationToken = default)
    {
        var (start, end) = NormalizeRange(filter);
        var page = ClampPage(filter);
        var pageSize = ClampPageSize(filter);
        var costs = await GetUnitCostsAsync(cancellationToken);

        var sold = await ApplySaleItemFilters(
                _saleItems.Query().AsNoTracking()
                    .Where(i => i.Sale != null && i.Sale.Status == CompletedSale && i.Sale.SaleDate >= start && i.Sale.SaleDate <= end),
                filter)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.TotalPrice) })
            .ToListAsync(cancellationToken);

        var purchased = await ApplyPurchaseItemFilters(
                _purchaseItems.Query().AsNoTracking()
                    .Where(i => i.Purchase != null && i.Purchase.Status == PurchaseStatus.Completed && i.Purchase.PurchaseDate >= start && i.Purchase.PurchaseDate <= end),
                filter)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToListAsync(cancellationToken);

        var soldMap = sold.ToDictionary(x => x.ProductId);
        var purchasedMap = purchased.ToDictionary(x => x.ProductId);

        var productsQuery = ApplyProductFilters(_products.Query().AsNoTracking().Include(p => p.Category), filter);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            productsQuery = productsQuery.Where(p => p.Name.ToLower().Contains(term) || p.SKU.ToLower().Contains(term));
        }

        var products = await productsQuery.ToListAsync(cancellationToken);
        var rows = products.Select(p =>
        {
            soldMap.TryGetValue(p.Id, out var s);
            purchasedMap.TryGetValue(p.Id, out var buy);
            var units = s?.Qty ?? 0;
            var revenue = s?.Revenue ?? 0m;
            var unitCost = costs.GetValueOrDefault(p.Id, p.Cost);
            return new ProductPerformanceRowDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                SKU = p.SKU,
                Category = p.Category?.Name ?? "",
                UnitsSold = units,
                SalesRevenue = revenue,
                PurchaseQuantity = buy?.Qty ?? 0,
                CurrentStock = p.QuantityInStock,
                Profit = Math.Round(revenue - (units * unitCost), 2)
            };
        }).ToList();

        IEnumerable<ProductPerformanceRowDto> ordered = filter.SortBy?.ToLowerInvariant() switch
        {
            "sku" => filter.Descending ? rows.OrderByDescending(r => r.SKU) : rows.OrderBy(r => r.SKU),
            "category" => filter.Descending ? rows.OrderByDescending(r => r.Category) : rows.OrderBy(r => r.Category),
            "unitssold" => filter.Descending ? rows.OrderByDescending(r => r.UnitsSold) : rows.OrderBy(r => r.UnitsSold),
            "revenue" => filter.Descending ? rows.OrderByDescending(r => r.SalesRevenue) : rows.OrderBy(r => r.SalesRevenue),
            "profit" => filter.Descending ? rows.OrderByDescending(r => r.Profit) : rows.OrderBy(r => r.Profit),
            "stock" => filter.Descending ? rows.OrderByDescending(r => r.CurrentStock) : rows.OrderBy(r => r.CurrentStock),
            _ => filter.Descending ? rows.OrderByDescending(r => r.SalesRevenue) : rows.OrderBy(r => r.SalesRevenue)
        };

        var list = ordered.ToList();
        return new ProductPerformanceReportDto
        {
            Table = Page(list.Skip((page - 1) * pageSize).Take(pageSize).ToList(), list.Count, page, pageSize),
            TopSelling = list.OrderByDescending(r => r.UnitsSold).Take(InsightLimit).ToList(),
            LeastSelling = list.Where(r => r.UnitsSold > 0).OrderBy(r => r.UnitsSold).Take(InsightLimit).ToList(),
            MostProfitable = list.OrderByDescending(r => r.Profit).Take(InsightLimit).ToList(),
            NoSales = list.Where(r => r.UnitsSold == 0).Take(InsightLimit).ToList()
        };
    }

    public async Task<CustomerReportDto> GetCustomersAsync(ReportFilterDto filter, CancellationToken cancellationToken = default)
    {
        var (start, end) = NormalizeRange(filter);
        var page = ClampPage(filter);
        var pageSize = ClampPageSize(filter);

        var sales = _sales.Query().AsNoTracking()
            .Where(s => s.Status == CompletedSale && s.CustomerId != null && s.SaleDate >= start && s.SaleDate <= end);
        if (filter.CustomerId.HasValue)
            sales = sales.Where(s => s.CustomerId == filter.CustomerId);

        var aggregates = await sales
            .GroupBy(s => s.CustomerId!.Value)
            .Select(g => new
            {
                CustomerId = g.Key,
                Count = g.Count(),
                Spent = g.Sum(x => x.TotalAmount),
                Last = g.Max(x => x.SaleDate)
            })
            .ToListAsync(cancellationToken);

        var customersQuery = _customers.Query().AsNoTracking();
        if (filter.CustomerId.HasValue)
            customersQuery = customersQuery.Where(c => c.Id == filter.CustomerId);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            customersQuery = customersQuery.Where(c =>
                c.Name.ToLower().Contains(term) || c.PhoneNumber.ToLower().Contains(term));
        }

        var customers = await customersQuery.ToListAsync(cancellationToken);
        var map = aggregates.ToDictionary(a => a.CustomerId);
        var rows = customers.Select(c =>
        {
            map.TryGetValue(c.Id, out var a);
            var count = a?.Count ?? 0;
            var spent = a?.Spent ?? 0m;
            return new CustomerReportRowDto
            {
                CustomerId = c.Id,
                CustomerName = c.Name,
                PhoneNumber = c.PhoneNumber,
                NumberOfPurchases = count,
                TotalAmountSpent = spent,
                AveragePurchaseValue = count == 0 ? 0m : Math.Round(spent / count, 2),
                LastPurchaseDate = a?.Last
            };
        }).ToList();

        var ordered = (filter.SortBy?.ToLowerInvariant() switch
        {
            "name" => filter.Descending ? rows.OrderByDescending(r => r.CustomerName) : rows.OrderBy(r => r.CustomerName),
            "count" => filter.Descending ? rows.OrderByDescending(r => r.NumberOfPurchases) : rows.OrderBy(r => r.NumberOfPurchases),
            "last" => filter.Descending ? rows.OrderByDescending(r => r.LastPurchaseDate) : rows.OrderBy(r => r.LastPurchaseDate),
            _ => filter.Descending ? rows.OrderByDescending(r => r.TotalAmountSpent) : rows.OrderBy(r => r.TotalAmountSpent)
        }).ToList();

        return new CustomerReportDto
        {
            Table = Page(ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList(), ordered.Count, page, pageSize),
            TopBySpending = ordered.OrderByDescending(r => r.TotalAmountSpent).Take(InsightLimit).ToList(),
            MostFrequent = ordered.OrderByDescending(r => r.NumberOfPurchases).Take(InsightLimit).ToList(),
            NoRecentPurchases = ordered.Where(r => r.LastPurchaseDate == null || r.LastPurchaseDate < start).Take(InsightLimit).ToList()
        };
    }

    public async Task<SupplierReportDto> GetSuppliersAsync(ReportFilterDto filter, CancellationToken cancellationToken = default)
    {
        var (start, end) = NormalizeRange(filter);
        var page = ClampPage(filter);
        var pageSize = ClampPageSize(filter);

        var purchases = CompletedPurchases(start, end);
        if (filter.SupplierId.HasValue)
            purchases = purchases.Where(p => p.SupplierId == filter.SupplierId);

        var aggregates = await purchases
            .GroupBy(p => p.SupplierId)
            .Select(g => new
            {
                SupplierId = g.Key,
                Count = g.Count(),
                Amount = g.Sum(x => x.TotalAmount),
                Last = g.Max(x => x.PurchaseDate)
            })
            .ToListAsync(cancellationToken);

        var suppliersQuery = _suppliers.Query().AsNoTracking();
        if (filter.SupplierId.HasValue)
            suppliersQuery = suppliersQuery.Where(s => s.Id == filter.SupplierId);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            suppliersQuery = suppliersQuery.Where(s =>
                s.Name.ToLower().Contains(term) || s.ContactName.ToLower().Contains(term));
        }

        var suppliers = await suppliersQuery.ToListAsync(cancellationToken);
        var productCounts = await _products.Query().AsNoTracking()
            .Where(p => p.SupplierId != null)
            .GroupBy(p => p.SupplierId!.Value)
            .Select(g => new { SupplierId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var productMap = productCounts.ToDictionary(x => x.SupplierId, x => x.Count);
        var map = aggregates.Where(a => a.SupplierId.HasValue).ToDictionary(a => a.SupplierId!.Value);

        var rows = suppliers.Select(s =>
        {
            map.TryGetValue(s.Id, out var a);
            return new SupplierReportRowDto
            {
                SupplierId = s.Id,
                SupplierName = s.Name,
                ContactPerson = s.ContactName,
                NumberOfPurchases = a?.Count ?? 0,
                TotalPurchaseAmount = a?.Amount ?? 0m,
                ProductsSupplied = productMap.GetValueOrDefault(s.Id),
                LastPurchaseDate = a?.Last
            };
        }).ToList();

        var ordered = (filter.SortBy?.ToLowerInvariant() switch
        {
            "name" => filter.Descending ? rows.OrderByDescending(r => r.SupplierName) : rows.OrderBy(r => r.SupplierName),
            "count" => filter.Descending ? rows.OrderByDescending(r => r.NumberOfPurchases) : rows.OrderBy(r => r.NumberOfPurchases),
            _ => filter.Descending ? rows.OrderByDescending(r => r.TotalPurchaseAmount) : rows.OrderBy(r => r.TotalPurchaseAmount)
        }).ToList();

        var historyQuery = _purchases.Query().AsNoTracking()
            .Include(p => p.PurchaseItems)
            .Where(p => p.PurchaseDate >= start && p.PurchaseDate <= end);
        if (filter.SupplierId.HasValue)
            historyQuery = historyQuery.Where(p => p.SupplierId == filter.SupplierId);

        var history = await historyQuery
            .OrderByDescending(p => p.PurchaseDate)
            .Take(12)
            .Select(p => new SupplierPurchaseHistoryRowDto
            {
                Id = p.Id,
                PurchaseNumber = p.PurchaseNumber,
                PurchaseDate = p.PurchaseDate,
                TotalAmount = p.TotalAmount,
                Status = p.Status,
                ItemsCount = p.PurchaseItems.Sum(i => i.Quantity)
            })
            .ToListAsync(cancellationToken);

        return new SupplierReportDto
        {
            Table = Page(ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList(), ordered.Count, page, pageSize),
            TopByAmount = ordered.OrderByDescending(r => r.TotalPurchaseAmount).Take(InsightLimit).ToList(),
            MostFrequent = ordered.OrderByDescending(r => r.NumberOfPurchases).Take(InsightLimit).ToList(),
            PurchaseHistory = history
        };
    }

    public async Task<ProfitReportDto> GetProfitAsync(ReportFilterDto filter, CancellationToken cancellationToken = default)
    {
        var (start, end) = NormalizeRange(filter);
        var costs = await GetUnitCostsAsync(cancellationToken);
        var revenue = await SumCompletedSalesAsync(start, end, filter, cancellationToken);
        var cogs = await SumCogsAsync(start, end, filter, costs, cancellationToken);
        var profit = revenue - cogs;
        var margin = revenue == 0 ? 0m : Math.Round(profit / revenue * 100m, 2);

        var dailySales = await DailySalesAsync(start, end, filter, cancellationToken);
        var dailyCogs = await DailyCogsAsync(start, end, filter, costs, cancellationToken);
        var cogsMap = dailyCogs.ToDictionary(x => x.Date, x => x.Amount);
        var profitSeries = dailySales.Select(d => new ChartPointDto
        {
            Label = d.Date.ToString("yyyy-MM-dd"),
            Value = Math.Round(d.Amount - cogsMap.GetValueOrDefault(d.Date), 2),
            Count = d.Count
        }).ToList();

        return new ProfitReportDto
        {
            TotalRevenue = revenue,
            TotalCogs = cogs,
            GrossProfit = profit,
            GrossProfitMargin = margin,
            ProfitOverTime = profitSeries
        };
    }

    public async Task<StockMovementReportDto> GetStockMovementsAsync(ReportFilterDto filter, CancellationToken cancellationToken = default)
    {
        var (start, end) = NormalizeRange(filter);
        var page = ClampPage(filter);
        var pageSize = ClampPageSize(filter);

        var query = _transactions.Query().AsNoTracking()
            .Include(t => t.Product)
            .Where(t => t.CreatedAt >= start && t.CreatedAt <= end);

        if (filter.ProductId.HasValue)
            query = query.Where(t => t.ProductId == filter.ProductId);
        if (!string.IsNullOrWhiteSpace(filter.UserId))
            query = query.Where(t => t.CreatedBy == filter.UserId);
        if (!string.IsNullOrWhiteSpace(filter.TransactionType)
            && Enum.TryParse<InventoryTransactionType>(filter.TransactionType, true, out var type))
        {
            query = query.Where(t => t.Type == type);
        }
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(t =>
                (t.Product != null && (t.Product.Name.ToLower().Contains(term) || t.Product.SKU.ToLower().Contains(term)))
                || (t.Notes != null && t.Notes.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var users = (await _userService.GetAllAsync(cancellationToken))
            .ToDictionary(u => u.Id, u => FormatUser(u.FirstName, u.LastName, u.UserName));

        var saleIds = items.Where(t => t.ReferenceType == "Sale" && t.ReferenceId.HasValue).Select(t => t.ReferenceId!.Value).Distinct().ToList();
        var purchaseIds = items.Where(t => t.ReferenceType == "Purchase" && t.ReferenceId.HasValue).Select(t => t.ReferenceId!.Value).Distinct().ToList();

        var saleNumbers = saleIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _sales.Query().AsNoTracking()
                .Where(s => saleIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.SaleNumber, cancellationToken);
        var purchaseNumbers = purchaseIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _purchases.Query().AsNoTracking()
                .Where(p => purchaseIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.PurchaseNumber, cancellationToken);

        var rows = items.Select(t =>
        {
            string? referenceNumber = null;
            if (t.ReferenceId.HasValue)
            {
                if (t.ReferenceType == "Sale")
                    saleNumbers.TryGetValue(t.ReferenceId.Value, out referenceNumber);
                else if (t.ReferenceType == "Purchase")
                    purchaseNumbers.TryGetValue(t.ReferenceId.Value, out referenceNumber);
            }

            return new StockMovementRowDto
            {
                Id = t.Id,
                Date = t.CreatedAt,
                Product = t.Product?.Name ?? "",
                TransactionType = t.Type.ToString(),
                QuantityChange = t.Quantity,
                PreviousStock = t.PreviousQuantity,
                NewStock = t.NewQuantity,
                ReferenceType = t.ReferenceType,
                ReferenceNumber = referenceNumber,
                PerformedBy = string.IsNullOrWhiteSpace(t.CreatedBy)
                    ? "System"
                    : users.GetValueOrDefault(t.CreatedBy, t.CreatedBy)
            };
        }).ToList();

        return new StockMovementReportDto
        {
            Table = Page(rows, totalCount, page, pageSize)
        };
    }

    public async Task<ReportExportFile> ExportAsync(string reportType, ReportFilterDto filter, string format, CancellationToken cancellationToken = default)
    {
        filter.Page = 1;
        filter.PageSize = ExportMaxRows;
        var kind = reportType.Trim().ToLowerInvariant();
        var fmt = format.Trim().ToLowerInvariant();

        var (headers, rows, title, summary) = kind switch
        {
            "sales" => await ExportSalesAsync(filter, cancellationToken),
            "purchases" => await ExportPurchasesAsync(filter, cancellationToken),
            "inventory" => await ExportInventoryAsync(filter, false, cancellationToken),
            "low-stock" => await ExportInventoryAsync(filter, true, cancellationToken),
            "products" => await ExportProductsAsync(filter, cancellationToken),
            "customers" => await ExportCustomersAsync(filter, cancellationToken),
            "suppliers" => await ExportSuppliersAsync(filter, cancellationToken),
            "profit" => await ExportProfitAsync(filter, cancellationToken),
            "stock-transactions" or "stock" => await ExportStockAsync(filter, cancellationToken),
            _ => throw new ArgumentException("Unknown report type.")
        };

        var (start, end) = NormalizeRange(filter);
        var meta = new List<string[]>
        {
            new[] { "Report", title },
            new[] { "Generated", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'") },
            new[] { "Date range", $"{start:yyyy-MM-dd} – {end:yyyy-MM-dd}" }
        };
        meta.AddRange(summary);

        return fmt is "xlsx" or "excel"
            ? BuildExcel(title, meta, headers, rows)
            : BuildCsv(title, meta, headers, rows);
    }

    // ── Inventory builders ─────────────────────────────────────────

    private async Task<InventoryReportDto> BuildInventoryAsync(ReportFilterDto filter, bool lowStockOnly, CancellationToken cancellationToken)
    {
        var page = ClampPage(filter);
        var pageSize = ClampPageSize(filter);
        var costs = await GetUnitCostsAsync(cancellationToken);

        var query = ApplyProductFilters(_products.Query().AsNoTracking().Include(p => p.Category), filter);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.SKU.ToLower().Contains(term));
        }
        if (lowStockOnly)
            query = query.Where(p => p.QuantityInStock <= p.MinimumStock);

        var products = await query.ToListAsync(cancellationToken);
        var rows = products.Select(p => MapInventoryRow(p, costs.GetValueOrDefault(p.Id, p.Cost))).ToList();

        IEnumerable<InventoryReportRowDto> ordered = filter.SortBy?.ToLowerInvariant() switch
        {
            "sku" => filter.Descending ? rows.OrderByDescending(r => r.SKU) : rows.OrderBy(r => r.SKU),
            "stock" => filter.Descending ? rows.OrderByDescending(r => r.CurrentStock) : rows.OrderBy(r => r.CurrentStock),
            "value" => filter.Descending ? rows.OrderByDescending(r => r.InventoryValue) : rows.OrderBy(r => r.InventoryValue),
            "status" => filter.Descending ? rows.OrderByDescending(r => r.StockStatus) : rows.OrderBy(r => r.StockStatus),
            _ => filter.Descending ? rows.OrderByDescending(r => r.Product) : rows.OrderBy(r => r.Product)
        };

        var list = ordered.ToList();

        InventoryReportSummaryDto summary;
        if (lowStockOnly)
        {
            summary = new InventoryReportSummaryDto
            {
                TotalProducts = list.Count,
                TotalStockQuantity = list.Sum(r => r.CurrentStock),
                InventoryValue = list.Sum(r => r.InventoryValue),
                LowStockProducts = list.Count,
                OutOfStockProducts = list.Count(r => r.StockStatus == "Out of Stock"),
                CriticalStockProducts = list.Count(r => r.StockStatus == "Critical Stock")
            };
        }
        else
        {
            summary = new InventoryReportSummaryDto
            {
                TotalProducts = products.Count,
                TotalStockQuantity = products.Sum(p => p.QuantityInStock),
                InventoryValue = products.Sum(p => p.QuantityInStock * costs.GetValueOrDefault(p.Id, p.Cost)),
                LowStockProducts = products.Count(p => p.QuantityInStock <= p.MinimumStock),
                OutOfStockProducts = products.Count(p => p.QuantityInStock == 0),
                CriticalStockProducts = list.Count(r => r.StockStatus == "Critical Stock")
            };
        }

        return new InventoryReportDto
        {
            Summary = summary,
            Table = Page(list.Skip((page - 1) * pageSize).Take(pageSize).ToList(), list.Count, page, pageSize)
        };
    }

    private static InventoryReportRowDto MapInventoryRow(Product p, decimal unitCost)
    {
        var status = p.QuantityInStock == 0
            ? "Out of Stock"
            : p.QuantityInStock <= Math.Max(1, p.MinimumStock / 2)
                ? "Critical Stock"
                : p.QuantityInStock <= p.MinimumStock
                    ? "Low Stock"
                    : "In Stock";

        var action = status switch
        {
            "Out of Stock" => "Create purchase immediately",
            "Critical Stock" => "Reorder as soon as possible",
            "Low Stock" => "Plan a replenishment purchase",
            _ => "No action required"
        };

        return new InventoryReportRowDto
        {
            ProductId = p.Id,
            Product = p.Name,
            SKU = p.SKU,
            Category = p.Category?.Name ?? "",
            CurrentStock = p.QuantityInStock,
            MinimumStock = p.MinimumStock,
            SellingPrice = p.Price,
            UnitCost = unitCost,
            InventoryValue = Math.Round(p.QuantityInStock * unitCost, 2),
            StockStatus = status,
            RecommendedAction = action
        };
    }

    // ── Aggregates ─────────────────────────────────────────────────

    private async Task<decimal> SumCompletedSalesAsync(DateTime start, DateTime end, ReportFilterDto filter, CancellationToken ct)
        => await ApplySaleFilters(CompletedSales(start, end), filter).SumAsync(s => (decimal?)s.TotalAmount, ct) ?? 0m;

    private async Task<decimal> SumCompletedPurchasesAsync(DateTime start, DateTime end, ReportFilterDto filter, CancellationToken ct)
        => await ApplyPurchaseFilters(CompletedPurchases(start, end), filter).SumAsync(p => (decimal?)p.TotalAmount, ct) ?? 0m;

    private async Task<decimal> SumSoldQuantityAsync(DateTime start, DateTime end, ReportFilterDto filter, CancellationToken ct)
        => await ApplySaleItemFilters(
                _saleItems.Query().AsNoTracking()
                    .Where(i => i.Sale != null && i.Sale.Status == CompletedSale && i.Sale.SaleDate >= start && i.Sale.SaleDate <= end),
                filter)
            .SumAsync(i => (int?)i.Quantity, ct) ?? 0;

    private async Task<decimal> SumCogsAsync(DateTime start, DateTime end, ReportFilterDto filter, Dictionary<Guid, decimal> costs, CancellationToken ct)
    {
        var items = await ApplySaleItemFilters(
                _saleItems.Query().AsNoTracking()
                    .Where(i => i.Sale != null && i.Sale.Status == CompletedSale && i.Sale.SaleDate >= start && i.Sale.SaleDate <= end),
                filter)
            .Select(i => new { i.ProductId, i.Quantity })
            .ToListAsync(ct);

        return Math.Round(items.Sum(i => i.Quantity * costs.GetValueOrDefault(i.ProductId)), 2);
    }

    private async Task<decimal> SumProfitAsync(DateTime start, DateTime end, ReportFilterDto filter, Dictionary<Guid, decimal> costs, CancellationToken ct)
    {
        var revenue = await SumCompletedSalesAsync(start, end, filter, ct);
        var cogs = await SumCogsAsync(start, end, filter, costs, ct);
        return revenue - cogs;
    }

    private async Task<List<DailyPoint>> DailySalesAsync(DateTime start, DateTime end, ReportFilterDto filter, CancellationToken ct)
    {
        var rows = await ApplySaleFilters(CompletedSales(start, end), filter)
            .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month, s.SaleDate.Day })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Day, Amount = g.Sum(x => x.TotalAmount), Count = g.Count() })
            .ToListAsync(ct);

        return FillDays(start, end, rows.Select(r => (new DateTime(r.Year, r.Month, r.Day, 0, 0, 0, DateTimeKind.Utc), r.Amount, r.Count)));
    }

    private async Task<List<DailyPoint>> DailyPurchasesAsync(DateTime start, DateTime end, ReportFilterDto filter, CancellationToken ct)
    {
        var rows = await ApplyPurchaseFilters(CompletedPurchases(start, end), filter)
            .GroupBy(p => new { p.PurchaseDate.Year, p.PurchaseDate.Month, p.PurchaseDate.Day })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Day, Amount = g.Sum(x => x.TotalAmount), Count = g.Count() })
            .ToListAsync(ct);

        return FillDays(start, end, rows.Select(r => (new DateTime(r.Year, r.Month, r.Day, 0, 0, 0, DateTimeKind.Utc), r.Amount, r.Count)));
    }

    private async Task<List<DailyPoint>> DailyCogsAsync(DateTime start, DateTime end, ReportFilterDto filter, Dictionary<Guid, decimal> costs, CancellationToken ct)
    {
        var items = await ApplySaleItemFilters(
                _saleItems.Query().AsNoTracking()
                    .Where(i => i.Sale != null && i.Sale.Status == CompletedSale && i.Sale.SaleDate >= start && i.Sale.SaleDate <= end),
                filter)
            .Select(i => new { i.Sale!.SaleDate, i.ProductId, i.Quantity })
            .ToListAsync(ct);

        var grouped = items
            .GroupBy(i => i.SaleDate.Date)
            .Select(g => (
                Date: DateTime.SpecifyKind(g.Key, DateTimeKind.Utc),
                Amount: g.Sum(x => x.Quantity * costs.GetValueOrDefault(x.ProductId)),
                Count: g.Count()));

        return FillDays(start, end, grouped);
    }

    private async Task<List<NamedAmountDto>> TopSoldProductsAsync(DateTime start, DateTime end, ReportFilterDto filter, int take, CancellationToken ct)
        => await ApplySaleItemFilters(
                _saleItems.Query().AsNoTracking()
                    .Where(i => i.Sale != null && i.Sale.Status == CompletedSale && i.Sale.SaleDate >= start && i.Sale.SaleDate <= end),
                filter)
            .GroupBy(i => new { i.ProductId, Name = i.ProductName })
            .Select(g => new NamedAmountDto
            {
                Id = g.Key.ProductId,
                Name = g.Key.Name,
                Quantity = g.Sum(x => x.Quantity),
                Amount = g.Sum(x => x.TotalPrice)
            })
            .OrderByDescending(x => x.Quantity)
            .Take(take)
            .ToListAsync(ct);

    private async Task<List<NamedAmountDto>> SalesByCategoryAsync(DateTime start, DateTime end, ReportFilterDto filter, CancellationToken ct)
        => await ApplySaleItemFilters(
                _saleItems.Query().AsNoTracking()
                    .Where(i => i.Sale != null && i.Sale.Status == CompletedSale && i.Sale.SaleDate >= start && i.Sale.SaleDate <= end),
                filter)
            .GroupBy(i => new
            {
                CategoryId = i.Product != null ? i.Product.CategoryId : Guid.Empty,
                Name = i.Product != null && i.Product.Category != null ? i.Product.Category.Name : "Uncategorized"
            })
            .Select(g => new NamedAmountDto
            {
                Id = g.Key.CategoryId,
                Name = g.Key.Name,
                Amount = g.Sum(x => x.TotalPrice),
                Quantity = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.Amount)
            .ToListAsync(ct);

    private async Task<(int Low, int Out)> StockCountsAsync(ReportFilterDto filter, CancellationToken ct)
    {
        var query = ApplyProductFilters(_products.Query().AsNoTracking(), filter);
        var low = await query.CountAsync(p => p.QuantityInStock <= p.MinimumStock, ct);
        var outOfStock = await query.CountAsync(p => p.QuantityInStock == 0, ct);
        return (low, outOfStock);
    }

    private async Task<Dictionary<Guid, decimal>> GetUnitCostsAsync(CancellationToken ct)
    {
        var fromPurchases = await _purchaseItems.Query().AsNoTracking()
            .Where(pi => pi.Purchase != null && pi.Purchase.Status == PurchaseStatus.Completed && pi.Quantity > 0)
            .GroupBy(pi => pi.ProductId)
            .Select(g => new { ProductId = g.Key, Cost = g.Sum(x => x.TotalCost) / g.Sum(x => x.Quantity) })
            .ToListAsync(ct);

        var productCosts = await _products.Query().AsNoTracking()
            .Select(p => new { p.Id, p.Cost })
            .ToListAsync(ct);

        var dict = productCosts.ToDictionary(p => p.Id, p => p.Cost);
        foreach (var row in fromPurchases)
            dict[row.ProductId] = row.Cost;
        return dict;
    }

    // ── Query filters ──────────────────────────────────────────────

    private IQueryable<Sale> CompletedSales(DateTime start, DateTime end)
        => _sales.Query().AsNoTracking().Where(s => s.Status == CompletedSale && s.SaleDate >= start && s.SaleDate <= end);

    private IQueryable<Purchase> CompletedPurchases(DateTime start, DateTime end)
        => _purchases.Query().AsNoTracking()
            .Where(p => p.Status == PurchaseStatus.Completed && p.PurchaseDate >= start && p.PurchaseDate <= end);

    private static IQueryable<Sale> ApplySaleFilters(IQueryable<Sale> query, ReportFilterDto filter)
    {
        if (filter.CustomerId.HasValue)
            query = query.Where(s => s.CustomerId == filter.CustomerId);
        if (filter.ProductId.HasValue)
            query = query.Where(s => s.SaleItems.Any(i => i.ProductId == filter.ProductId));
        if (filter.CategoryId.HasValue)
            query = query.Where(s => s.SaleItems.Any(i => i.Product != null && i.Product.CategoryId == filter.CategoryId));
        return query;
    }

    private static IQueryable<SaleItem> ApplySaleItemFilters(IQueryable<SaleItem> query, ReportFilterDto filter)
    {
        if (filter.CustomerId.HasValue)
            query = query.Where(i => i.Sale != null && i.Sale.CustomerId == filter.CustomerId);
        if (filter.ProductId.HasValue)
            query = query.Where(i => i.ProductId == filter.ProductId);
        if (filter.CategoryId.HasValue)
            query = query.Where(i => i.Product != null && i.Product.CategoryId == filter.CategoryId);
        return query;
    }

    private static IQueryable<Purchase> ApplyPurchaseFilters(IQueryable<Purchase> query, ReportFilterDto filter)
    {
        if (filter.SupplierId.HasValue)
            query = query.Where(p => p.SupplierId == filter.SupplierId);
        if (filter.ProductId.HasValue)
            query = query.Where(p => p.PurchaseItems.Any(i => i.ProductId == filter.ProductId));
        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.PurchaseItems.Any(i => i.Product != null && i.Product.CategoryId == filter.CategoryId));
        return query;
    }

    private static IQueryable<PurchaseItem> ApplyPurchaseItemFilters(IQueryable<PurchaseItem> query, ReportFilterDto filter)
    {
        if (filter.SupplierId.HasValue)
            query = query.Where(i => i.Purchase != null && i.Purchase.SupplierId == filter.SupplierId);
        if (filter.ProductId.HasValue)
            query = query.Where(i => i.ProductId == filter.ProductId);
        if (filter.CategoryId.HasValue)
            query = query.Where(i => i.Product != null && i.Product.CategoryId == filter.CategoryId);
        return query;
    }

    private static IQueryable<Product> ApplyProductFilters(IQueryable<Product> query, ReportFilterDto filter)
    {
        if (filter.ProductId.HasValue)
            query = query.Where(p => p.Id == filter.ProductId);
        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == filter.CategoryId);
        if (filter.SupplierId.HasValue)
            query = query.Where(p => p.SupplierId == filter.SupplierId);
        return query;
    }

    private static IQueryable<Sale> SortSales(IQueryable<Sale> query, ReportFilterDto filter)
        => filter.SortBy?.ToLowerInvariant() switch
        {
            "invoicenumber" => filter.Descending ? query.OrderByDescending(s => s.SaleNumber) : query.OrderBy(s => s.SaleNumber),
            "customer" => filter.Descending ? query.OrderByDescending(s => s.CustomerName) : query.OrderBy(s => s.CustomerName),
            "amount" => filter.Descending ? query.OrderByDescending(s => s.TotalAmount) : query.OrderBy(s => s.TotalAmount),
            "status" => filter.Descending ? query.OrderByDescending(s => s.Status) : query.OrderBy(s => s.Status),
            _ => filter.Descending ? query.OrderByDescending(s => s.SaleDate) : query.OrderBy(s => s.SaleDate)
        };

    // ── Export payloads ────────────────────────────────────────────

    private async Task<(string[] Headers, List<string[]> Rows, string Title, List<string[]> Summary)> ExportSalesAsync(ReportFilterDto filter, CancellationToken ct)
    {
        var data = await GetSalesAsync(filter, ct);
        var rows = data.Table.Items.Select(r => new[]
        {
            r.InvoiceNumber, r.Customer, r.Date.ToString("yyyy-MM-dd"), r.NumberOfItems.ToString(),
            r.TotalAmount.ToString("0.00"), r.PaymentMethod, r.Status
        }).ToList();
        return (
            new[] { "Invoice Number", "Customer", "Date", "Items", "Total Amount", "Payment Method", "Status" },
            rows,
            "Sales Report",
            new List<string[]>
            {
                new[] { "Total sales", data.Summary.TotalSalesAmount.ToString("0.00") },
                new[] { "Number of sales", data.Summary.NumberOfSales.ToString() },
                new[] { "Products sold", data.Summary.TotalProductsSold.ToString() },
                new[] { "Average sale", data.Summary.AverageSaleValue.ToString("0.00") },
                new[] { "Discounts", data.Summary.TotalDiscounts.ToString("0.00") },
                new[] { "Tax", data.Summary.TotalTax.ToString("0.00") }
            });
    }

    private async Task<(string[] Headers, List<string[]> Rows, string Title, List<string[]> Summary)> ExportPurchasesAsync(ReportFilterDto filter, CancellationToken ct)
    {
        var data = await GetPurchasesAsync(filter, ct);
        var rows = data.Table.Items.Select(r => new[]
        {
            r.PurchaseNumber, r.Supplier, r.PurchaseDate.ToString("yyyy-MM-dd"), r.NumberOfItems.ToString(),
            r.TotalAmount.ToString("0.00"), r.Status
        }).ToList();
        return (
            new[] { "Purchase Number", "Supplier", "Date", "Items", "Total Amount", "Status" },
            rows,
            "Purchase Report",
            new List<string[]>
            {
                new[] { "Total purchases", data.Summary.TotalPurchaseAmount.ToString("0.00") },
                new[] { "Number of purchases", data.Summary.NumberOfPurchases.ToString() },
                new[] { "Products purchased", data.Summary.TotalProductsPurchased.ToString() },
                new[] { "Average purchase", data.Summary.AveragePurchaseValue.ToString("0.00") }
            });
    }

    private async Task<(string[] Headers, List<string[]> Rows, string Title, List<string[]> Summary)> ExportInventoryAsync(ReportFilterDto filter, bool lowOnly, CancellationToken ct)
    {
        var data = lowOnly ? (await GetLowStockAsync(filter, ct)).Table : (await GetInventoryAsync(filter, ct)).Table;
        var rows = data.Items.Select(r => new[]
        {
            r.Product, r.SKU, r.Category, r.CurrentStock.ToString(), r.MinimumStock.ToString(),
            r.SellingPrice.ToString("0.00"), r.InventoryValue.ToString("0.00"), r.StockStatus, r.RecommendedAction
        }).ToList();
        return (
            new[] { "Product", "SKU", "Category", "Current Stock", "Minimum Stock", "Selling Price", "Inventory Value", "Status", "Action" },
            rows,
            lowOnly ? "Low Stock Report" : "Inventory Report",
            new List<string[]> { new[] { "Rows", data.TotalCount.ToString() } });
    }

    private async Task<(string[] Headers, List<string[]> Rows, string Title, List<string[]> Summary)> ExportProductsAsync(ReportFilterDto filter, CancellationToken ct)
    {
        var data = await GetProductsAsync(filter, ct);
        var rows = data.Table.Items.Select(r => new[]
        {
            r.ProductName, r.SKU, r.Category, r.UnitsSold.ToString(), r.SalesRevenue.ToString("0.00"),
            r.PurchaseQuantity.ToString(), r.CurrentStock.ToString(), r.Profit.ToString("0.00")
        }).ToList();
        return (
            new[] { "Product", "SKU", "Category", "Units Sold", "Sales Revenue", "Purchase Qty", "Current Stock", "Profit" },
            rows, "Product Performance Report",
            new List<string[]> { new[] { "Rows", data.Table.TotalCount.ToString() } });
    }

    private async Task<(string[] Headers, List<string[]> Rows, string Title, List<string[]> Summary)> ExportCustomersAsync(ReportFilterDto filter, CancellationToken ct)
    {
        var data = await GetCustomersAsync(filter, ct);
        var rows = data.Table.Items.Select(r => new[]
        {
            r.CustomerName, r.PhoneNumber, r.NumberOfPurchases.ToString(), r.TotalAmountSpent.ToString("0.00"),
            r.AveragePurchaseValue.ToString("0.00"), r.LastPurchaseDate?.ToString("yyyy-MM-dd") ?? ""
        }).ToList();
        return (
            new[] { "Customer", "Phone", "Purchases", "Total Spent", "Average", "Last Purchase" },
            rows, "Customer Report",
            new List<string[]> { new[] { "Rows", data.Table.TotalCount.ToString() } });
    }

    private async Task<(string[] Headers, List<string[]> Rows, string Title, List<string[]> Summary)> ExportSuppliersAsync(ReportFilterDto filter, CancellationToken ct)
    {
        var data = await GetSuppliersAsync(filter, ct);
        var rows = data.Table.Items.Select(r => new[]
        {
            r.SupplierName, r.ContactPerson, r.NumberOfPurchases.ToString(), r.TotalPurchaseAmount.ToString("0.00"),
            r.ProductsSupplied.ToString(), r.LastPurchaseDate?.ToString("yyyy-MM-dd") ?? ""
        }).ToList();
        return (
            new[] { "Supplier", "Contact", "Purchases", "Total Amount", "Products Supplied", "Last Purchase" },
            rows, "Supplier Report",
            new List<string[]> { new[] { "Rows", data.Table.TotalCount.ToString() } });
    }

    private async Task<(string[] Headers, List<string[]> Rows, string Title, List<string[]> Summary)> ExportProfitAsync(ReportFilterDto filter, CancellationToken ct)
    {
        var data = await GetProfitAsync(filter, ct);
        var rows = data.ProfitOverTime.Select(r => new[] { r.Label, r.Value.ToString("0.00") }).ToList();
        return (
            new[] { "Date", "Gross Profit" },
            rows,
            "Profit Report",
            new List<string[]>
            {
                new[] { "Revenue", data.TotalRevenue.ToString("0.00") },
                new[] { "COGS", data.TotalCogs.ToString("0.00") },
                new[] { "Gross profit", data.GrossProfit.ToString("0.00") },
                new[] { "Gross margin %", data.GrossProfitMargin.ToString("0.00") },
                new[] { "Cost basis", data.CostBasis }
            });
    }

    private async Task<(string[] Headers, List<string[]> Rows, string Title, List<string[]> Summary)> ExportStockAsync(ReportFilterDto filter, CancellationToken ct)
    {
        var data = await GetStockMovementsAsync(filter, ct);
        var rows = data.Table.Items.Select(r => new[]
        {
            r.Date.ToString("yyyy-MM-dd HH:mm"), r.Product, r.TransactionType, r.QuantityChange.ToString(),
            r.PreviousStock.ToString(), r.NewStock.ToString(), r.ReferenceType ?? "", r.ReferenceNumber ?? "", r.PerformedBy
        }).ToList();
        return (
            new[] { "Date", "Product", "Type", "Qty Change", "Previous", "New", "Reference Type", "Reference Number", "Performed By" },
            rows, "Stock Movement Report",
            new List<string[]> { new[] { "Rows", data.Table.TotalCount.ToString() } });
    }

    // ── File builders ──────────────────────────────────────────────

    private static ReportExportFile BuildCsv(string title, List<string[]> meta, string[] headers, List<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        foreach (var line in meta)
            sb.AppendLine(string.Join(",", line.Select(Csv)));
        sb.AppendLine();
        sb.AppendLine(string.Join(",", headers.Select(Csv)));
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", row.Select(Csv)));

        var slug = Slug(title);
        return new ReportExportFile
        {
            Content = Encoding.UTF8.GetBytes(sb.ToString()),
            ContentType = "text/csv; charset=utf-8",
            FileName = $"{slug}-{DateTime.UtcNow:yyyyMMdd}.csv"
        };
    }

    private static ReportExportFile BuildExcel(string title, List<string[]> meta, string[] headers, List<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.Append("""
            <?xml version="1.0"?>
            <?mso-application progid="Excel.Sheet"?>
            <Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet" xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">
            <Worksheet ss:Name="Report"><Table>
            """);

        void Row(IEnumerable<string> cells)
        {
            sb.Append("<Row>");
            foreach (var cell in cells)
                sb.Append($"<Cell><Data ss:Type=\"String\">{System.Security.SecurityElement.Escape(cell)}</Data></Cell>");
            sb.Append("</Row>");
        }

        foreach (var line in meta)
            Row(line);
        Row(Array.Empty<string>());
        Row(headers);
        foreach (var row in rows)
            Row(row);

        sb.Append("</Table></Worksheet></Workbook>");
        return new ReportExportFile
        {
            Content = Encoding.UTF8.GetBytes(sb.ToString()),
            ContentType = "application/vnd.ms-excel",
            FileName = $"{Slug(title)}-{DateTime.UtcNow:yyyyMMdd}.xls"
        };
    }

    // ── Helpers ────────────────────────────────────────────────────

    internal static (DateTime Start, DateTime End) NormalizeRange(ReportFilterDto filter)
    {
        var now = DateTime.UtcNow;
        var start = filter.StartDate.HasValue
            ? DateTime.SpecifyKind(filter.StartDate.Value.Date, DateTimeKind.Utc)
            : new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = filter.EndDate?.Date ?? now.Date;
        var end = DateTime.SpecifyKind(endDate.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
        if (start > end)
            throw new ArgumentException("Start Date must be less than or equal to End Date.");
        return (start, end);
    }

    private static (DateTime Start, DateTime End) PreviousRange(DateTime start, DateTime end)
    {
        var duration = end - start;
        var prevEnd = start.AddTicks(-1);
        return (prevEnd - duration, prevEnd);
    }

    private static PeriodMetricDto Metric(decimal current, decimal previous) => new()
    {
        Value = current,
        PreviousValue = previous,
        ChangePercent = previous == 0
            ? current == 0 ? 0 : 100
            : Math.Round((current - previous) / previous * 100m, 1)
    };

    private static int ClampPage(ReportFilterDto filter) => filter.Page < 1 ? 1 : filter.Page;

    private static int ClampPageSize(ReportFilterDto filter)
        => filter.PageSize < 1 ? 20 : Math.Min(filter.PageSize, ExportMaxRows);

    private static PagedResponse<T> Page<T>(IReadOnlyList<T> items, int total, int page, int pageSize) => new()
    {
        Items = items,
        TotalCount = total,
        Page = page,
        PageSize = pageSize
    };

    private static List<ChartPointDto> ToChart(IEnumerable<DailyPoint> points)
        => points.Select(p => new ChartPointDto
        {
            Label = p.Date.ToString("yyyy-MM-dd"),
            Value = p.Amount,
            Count = p.Count
        }).ToList();

    private static List<ChartPointDto> RollupWeeks(IEnumerable<DailyPoint> daily)
        => daily
            .GroupBy(p =>
            {
                var diff = (7 + (p.Date.DayOfWeek - DayOfWeek.Monday)) % 7;
                return p.Date.AddDays(-diff).Date;
            })
            .OrderBy(g => g.Key)
            .Select(g => new ChartPointDto
            {
                Label = $"Week of {g.Key:yyyy-MM-dd}",
                Value = g.Sum(x => x.Amount),
                Count = g.Sum(x => x.Count)
            })
            .ToList();

    private static List<ChartPointDto> RollupMonths(IEnumerable<DailyPoint> daily)
        => daily
            .GroupBy(p => new { p.Date.Year, p.Date.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new ChartPointDto
            {
                Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy", CultureInfo.InvariantCulture),
                Value = g.Sum(x => x.Amount),
                Count = g.Sum(x => x.Count)
            })
            .ToList();

    private static List<DailyPoint> FillDays(DateTime start, DateTime end, IEnumerable<(DateTime Date, decimal Amount, int Count)> values)
    {
        var map = values.ToDictionary(v => v.Date.Date, v => v);
        var list = new List<DailyPoint>();
        for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
        {
            map.TryGetValue(d, out var hit);
            list.Add(new DailyPoint
            {
                Date = DateTime.SpecifyKind(d, DateTimeKind.Utc),
                Amount = hit.Amount,
                Count = hit.Count
            });
        }
        return list;
    }

    private static string Csv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string Slug(string title)
        => string.Join("-", title.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string FormatUser(string first, string last, string userName)
    {
        var name = $"{first} {last}".Trim();
        return string.IsNullOrWhiteSpace(name) ? userName : name;
    }

    private sealed class DailyPoint
    {
        public DateTime Date { get; init; }
        public decimal Amount { get; init; }
        public int Count { get; init; }
    }
}
