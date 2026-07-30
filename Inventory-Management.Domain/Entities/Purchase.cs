using System;
using System.Collections.Generic;

namespace Inventory_Management.Domain.Entities;

public class Purchase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    
    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();
}
