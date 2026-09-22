namespace Inventory_Management.Application.Features.CustomerBottles.DTOs;

public class CustomerBottleBalanceDto
{
    public Guid Id { get; set; }
    public Guid? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid BottleTypeId { get; set; }
    public string BottleTypeName { get; set; } = string.Empty;
    public int Balance { get; set; }
    public decimal TotalDeposit { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}
