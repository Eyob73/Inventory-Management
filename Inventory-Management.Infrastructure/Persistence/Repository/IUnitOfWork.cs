namespace Inventory_Management.Infrastructure.Persistence.Repository
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();
    }
}
