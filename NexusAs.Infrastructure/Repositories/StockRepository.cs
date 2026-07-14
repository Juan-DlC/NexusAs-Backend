using Microsoft.EntityFrameworkCore;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Entities;
using NexusAs.Infrastructure.Data;

namespace NexusAs.Infrastructure.Repositories
{
    public class StockRepository : BaseRepository<StockMovement>, IStockRepository
    {
        public StockRepository(NexusAsDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<StockMovement>> GetMovementsByProductAsync(int productId)
        {
            return await _context.StockMovements
                .Include(m => m.Product)
                .Include(m => m.User)
                .Where(m => m.ProductId == productId)
                .OrderByDescending(m => m.Date)
                .ToListAsync();
        }
    }
}