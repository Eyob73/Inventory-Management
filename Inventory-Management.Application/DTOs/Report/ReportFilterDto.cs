namespace Inventory_Management.Application.DTOs.Report;

public class ReportFilterDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? TransactionType { get; set; }
    public string? UserId { get; set; }
    public string? SortBy { get; set; }
    public bool Descending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string Format { get; set; } = "json";
}
