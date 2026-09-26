using Microsoft.EntityFrameworkCore;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Entities;
using NexusAs.Infrastructure.Data;

namespace NexusAs.Infrastructure.Repositories
{
    public class ReturnRepository : BaseRepository<Return>, IReturnRepository
    {
        public ReturnRepository(NexusAsDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Return>> GetAllWithDetailsAsync(DateTime? from = null, DateTime? to = null)
        {
            var query = _context.Returns
                .AsNoTracking()
                .Include(r => r.ReturnDetails.Where(rd => rd.IsActive))
                    .ThenInclude(rd => rd.Product)
                .Include(r => r.User)
                .Include(r => r.Sale)
                .Where(r => r.IsActive &&
                    (from == null || r.Date >= from) &&
                    (to == null || r.Date <= to));

            return await query.ToListAsync();
        }
    }
}
