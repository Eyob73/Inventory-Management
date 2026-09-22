using Inventory_Management.Domain.Entities;
namespace Inventory_Management.Application.Features.BottleInventory.DTOs;

public class BottleInventoryDto
{
    public Guid Id { get; set; }
    public Guid BottleTypeId { get; set; }
    public string BottleTypeName { get; set; } = string.Empty;
    public int FullBottles { get; set; }
    public int EmptyBottles { get; set; }
    public int DamagedBottles { get; set; }
    public int LostBottles { get; set; }
    public int WithCustomers { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}
