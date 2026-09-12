using Inventory_Management.Application.DTOs.Report;
using Inventory_Management.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Inventory_Management.Api.Controllers;

[Authorize(Roles = "Admin,Manager,Sales")]
[ApiController]
[Route("api/reports")]
[Tags("Reports")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports)
    {
        _reports = reports;
    }

    [HttpGet("dashboard")]
    [EndpointSummary("Reports dashboard summary")]
    [ProducesResponseType(typeof(DashboardReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<DashboardReportDto>> Dashboard([FromQuery] ReportFilterDto filter, CancellationToken cancellationToken)
        => Execute(filter, () => _reports.GetDashboardAsync(filter, cancellationToken));

    [HttpGet("sales")]
    [EndpointSummary("Sales report")]
    [ProducesResponseType(typeof(SalesReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<SalesReportDto>> Sales([FromQuery] ReportFilterDto filter, CancellationToken cancellationToken)
        => Execute(filter, () => _reports.GetSalesAsync(filter, cancellationToken));

    [HttpGet("purchases")]
    [Authorize(Roles = "Admin,Manager")]
    [EndpointSummary("Purchase report")]
    [ProducesResponseType(typeof(PurchasesReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<PurchasesReportDto>> Purchases([FromQuery] ReportFilterDto filter, CancellationToken cancellationToken)
        => Execute(filter, () => _reports.GetPurchasesAsync(filter, cancellationToken));

    [HttpGet("inventory")]
    [Authorize(Roles = "Admin,Manager")]
    [EndpointSummary("Inventory report")]
    [ProducesResponseType(typeof(InventoryReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<InventoryReportDto>> Inventory([FromQuery] ReportFilterDto filter, CancellationToken cancellationToken)
        => Execute(filter, () => _reports.GetInventoryAsync(filter, cancellationToken));

    [HttpGet("low-stock")]
    [Authorize(Roles = "Admin,Manager")]
    [EndpointSummary("Low stock report")]
    [ProducesResponseType(typeof(LowStockReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<LowStockReportDto>> LowStock([FromQuery] ReportFilterDto filter, CancellationToken cancellationToken)
        => Execute(filter, () => _reports.GetLowStockAsync(filter, cancellationToken));

    [HttpGet("products")]
    [EndpointSummary("Product performance report")]
    [ProducesResponseType(typeof(ProductPerformanceReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ProductPerformanceReportDto>> Products([FromQuery] ReportFilterDto filter, CancellationToken cancellationToken)
        => Execute(filter, () => _reports.GetProductsAsync(filter, cancellationToken));

    [HttpGet("customers")]
    [EndpointSummary("Customer report")]
    [ProducesResponseType(typeof(CustomerReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<CustomerReportDto>> Customers([FromQuery] ReportFilterDto filter, CancellationToken cancellationToken)
        => Execute(filter, () => _reports.GetCustomersAsync(filter, cancellationToken));

    [HttpGet("suppliers")]
    [Authorize(Roles = "Admin,Manager")]
    [EndpointSummary("Supplier report")]
    [ProducesResponseType(typeof(SupplierReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<SupplierReportDto>> Suppliers([FromQuery] ReportFilterDto filter, CancellationToken cancellationToken)
        => Execute(filter, () => _reports.GetSuppliersAsync(filter, cancellationToken));

    [HttpGet("profit")]
    [Authorize(Roles = "Admin,Manager")]
    [EndpointSummary("Profit report")]
    [ProducesResponseType(typeof(ProfitReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ProfitReportDto>> Profit([FromQuery] ReportFilterDto filter, CancellationToken cancellationToken)
        => Execute(filter, () => _reports.GetProfitAsync(filter, cancellationToken));

    [HttpGet("stock-transactions")]
    [EndpointSummary("Stock movement report")]
    [ProducesResponseType(typeof(StockMovementReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<StockMovementReportDto>> StockTransactions([FromQuery] ReportFilterDto filter, CancellationToken cancellationToken)
        => Execute(filter, () => _reports.GetStockMovementsAsync(filter, cancellationToken));

    [HttpGet("export/{reportType}")]
    [Authorize(Roles = "Admin,Manager")]
    [EndpointSummary("Export a report as CSV or Excel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Export(
        string reportType,
        [FromQuery] ReportFilterDto filter,
        [FromQuery] string format = "csv",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var file = await _reports.ExportAsync(reportType, filter, format, cancellationToken);
            return File(file.Content, file.ContentType, file.FileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }

    private async Task<ActionResult<T>> Execute<T>(ReportFilterDto filter, Func<Task<T>> action)
    {
        if (User.IsInRole("Sales") && !User.IsInRole("Admin") && !User.IsInRole("Manager"))
        {
            filter.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        try
        {
            return Ok(await action());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }
}
