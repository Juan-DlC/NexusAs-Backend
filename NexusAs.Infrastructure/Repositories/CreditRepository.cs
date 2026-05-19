using Microsoft.EntityFrameworkCore;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Entities;
using NexusAs.Infrastructure.Data;

namespace NexusAs.Infrastructure.Repositories
{
    public class CreditRepository : BaseRepository<Credit>, ICreditRepository
    {
        public CreditRepository(NexusAsDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Credit>> GetAllWithDetailsAsync(string? status = null)
        {
            return await _context.Credits
                .Include(c => c.Sale)
                .Include(c => c.Customer)
                .Where(c => c.IsActive &&
                    (status == null || c.Status.ToString() == status))
                .ToListAsync();
        }

        public async Task<Credit?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Credits
                .Include(c => c.Sale)
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
        }
    }
}