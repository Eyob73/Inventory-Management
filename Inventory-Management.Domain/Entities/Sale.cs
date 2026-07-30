using System;
using System.Collections.Generic;

namespace Inventory_Management.Domain.Entities;

public class Sale
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
