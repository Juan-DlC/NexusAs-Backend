using NexusAs.Domain.Entities;

namespace NexusAs.Application.Interfaces
{
    public interface IStockRepository : IRepository<StockMovement>
    {
        Task<IEnumerable<StockMovement>> GetMovementsByProductAsync(int productId);
    }
}