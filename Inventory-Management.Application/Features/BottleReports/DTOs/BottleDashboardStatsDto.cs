namespace Inventory_Management.Application.Features.BottleReports.DTOs;

public class BottleDashboardStatsDto
{
    public int TotalBottleTypes { get; set; }
    public int TotalFullBottles { get; set; }
    public int TotalEmptyBottles { get; set; }
    public int TotalBottlesWithCustomers { get; set; }
    public decimal TotalPendingDepositLiability { get; set; }
    public int TotalDamagedBottles { get; set; }
    public int TotalLostBottles { get; set; }
}
