using Inventory_Management.Application.DTOs.Report;

namespace Inventory_Management.Application.Interfaces.Services;

public interface IReportService
{
    Task<DashboardReportDto> GetDashboardAsync(ReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<SalesReportDto> GetSalesAsync(ReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<PurchasesReportDto> GetPurchasesAsync(ReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<InventoryReportDto> GetInventoryAsync(ReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<LowStockReportDto> GetLowStockAsync(ReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<ProductPerformanceReportDto> GetProductsAsync(ReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<CustomerReportDto> GetCustomersAsync(ReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<SupplierReportDto> GetSuppliersAsync(ReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<ProfitReportDto> GetProfitAsync(ReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<StockMovementReportDto> GetStockMovementsAsync(ReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<ReportExportFile> ExportAsync(string reportType, ReportFilterDto filter, string format, CancellationToken cancellationToken = default);
}
