namespace Inventory_Management.Application.Features.BottleTransactions.DTOs;

public class BottleTransactionDto
{
    public Guid Id { get; set; }
    public Guid BottleTypeId { get; set; }
    public string BottleTypeName { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal DepositAmount { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
