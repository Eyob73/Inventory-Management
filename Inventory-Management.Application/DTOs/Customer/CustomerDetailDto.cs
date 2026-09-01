namespace Inventory_Management.Application.DTOs.Customer;

public class CustomerDetailDto : CustomerDto
{
    public List<CustomerSaleDto> SalesHistory { get; set; } = new();
}

public class CustomerSaleDto
{
    public Guid Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public int ItemsCount { get; set; }
    public string Status { get; set; } = "Completed";
    public List<CustomerSaleItemDto> Items { get; set; } = new();
}

public class CustomerSaleItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
