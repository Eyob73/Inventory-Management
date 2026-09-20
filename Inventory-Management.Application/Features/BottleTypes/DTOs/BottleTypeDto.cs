namespace Inventory_Management.Application.Features.BottleTypes.DTOs;

public class BottleTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DepositAmount { get; set; }
    public string Capacity { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Quantity { get; set; }
}

