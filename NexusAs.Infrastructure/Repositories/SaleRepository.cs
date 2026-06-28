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
        public async Task<(IEnumerable<Sale> Items, int TotalRecords)> GetSalesWithDetailsAsync(
            int userId, string userRole, DateTime? from, DateTime? to,
            string? search, int pageNumber, int pageSize)
        {
            var query = _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.User)
                .Where(s => s.IsActive &&
                    (userRole == "Admin" || s.UserId == userId) &&
                    (from == null || s.Date >= from) &&
                    (to == null || s.Date <= to));

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s =>
                    s.SaleNumber.Contains(search) ||
                    (s.Customer != null && s.Customer.Name.Contains(search)));
            }

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(s => s.Date)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalRecords);
        }

        public async Task<Sale?> GetSaleByIdWithDetailsAsync(int id)
        {
            return await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.User)
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Product)
                .Include(s => s.Credit)
                    .ThenInclude(c => c!.Installments)
                .FirstOrDefaultAsync(s => s.Id == id);
        }
    }

}