using Inventory_Management.Domain.Entities;

namespace Inventory_Management.Infrastructure.Persistence.Repository
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        // Add specific methods for Product repository here
    }
}
