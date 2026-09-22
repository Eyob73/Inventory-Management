namespace Inventory_Management.Domain.Enums;

public enum InventoryTransactionType
{
    Purchase = 0,
    Sale = 1,
    Adjustment = 2,
    Return = 3,
    PurchaseReversal = 4,
    SaleReturn = 5
}
