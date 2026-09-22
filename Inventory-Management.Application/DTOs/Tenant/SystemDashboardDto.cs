namespace Inventory_Management.Application.DTOs.Tenant;

public class SystemDashboardDto
{
    public int TotalCompanies { get; set; }
    public int ActiveCompanies { get; set; }
    public int SuspendedCompanies { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
}
