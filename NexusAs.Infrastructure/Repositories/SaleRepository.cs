using Microsoft.EntityFrameworkCore;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Entities;
using NexusAs.Infrastructure.Data;

namespace NexusAs.Infrastructure.Repositories
{
    public class SaleRepository : BaseRepository<Sale>, ISaleRepository
    {
        public SaleRepository(NexusAsDbContext context) : base(context)
        {
        }
        public async Task<IEnumerable<Sale>> GetSalesWithDetailsAsync(
            int userId, string userRole, DateTime? from, DateTime? to)
        {
            return await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.User)
                .Where(s => s.IsActive &&
                    (userRole == "Admin" || s.UserId == userId) &&
                    (from == null || s.Date >= from) &&
                    (to == null || s.Date <= to))
                .OrderByDescending(s => s.Date)
                .ToListAsync();
        }

        public async Task<Sale?> GetSaleByIdWithDetailsAsync(int id)
        {
            return await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.User)
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Product)
                .FirstOrDefaultAsync(s => s.Id == id);
        }
    }

}