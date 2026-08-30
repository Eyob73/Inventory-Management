namespace Inventory_Management.Application.DTOs.Sale;

public class SaleFilterDto
{
    public string? SearchTerm { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Status { get; set; }
    public string? UserId { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
