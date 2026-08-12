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
            // BUG 3 FIX: Incluir Sale.User para obtener nombre del vendedor/socia
            return await _context.Credits
                .Include(c => c.Sale)
                    .ThenInclude(s => s!.User)
                .Include(c => c.Customer)
                .Where(c => c.IsActive &&
                    (status == null || c.Status.ToString() == status))
                .ToListAsync();
        }

        public async Task<Credit?> GetByIdWithDetailsAsync(int id)
        {
            // BUG 3 FIX: Incluir Sale.User para obtener nombre del vendedor/socia
            return await _context.Credits
                .Include(c => c.Sale)
                    .ThenInclude(s => s!.User)
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
        }

        public async Task<Credit?> GetByIdFullDetailsAsync(int id)
        {
            return await _context.Credits
                .Include(c => c.Sale)
                    .ThenInclude(s => s!.SaleDetails)
                        .ThenInclude(sd => sd.Product)
                .Include(c => c.Customer)
                .Include(c => c.Payments)
                    .ThenInclude(p => p.User)
                .Include(c => c.Installments)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
        }

        public async Task<(IEnumerable<Credit> Items, int TotalRecords)> GetAllPagedAsync(
     string? status, string? search, int pageNumber, int pageSize)
        {
            NexusAs.Domain.Enums.CreditStatus? statusEnum = null;
            if (!string.IsNullOrEmpty(status) &&
                Enum.TryParse<NexusAs.Domain.Enums.CreditStatus>(status, out var parsed))
            {
                statusEnum = parsed;
            }

            // BUG 3 FIX: Incluir Sale.User para obtener nombre del vendedor/socia
            var query = _context.Credits
                .Include(c => c.Sale)
                    .ThenInclude(s => s!.User)
                .Include(c => c.Customer)
                .Where(c => c.IsActive &&
                    (statusEnum == null || c.Status == statusEnum));

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    (c.Sale != null && c.Sale.SaleNumber.Contains(search)) ||
                    (c.Customer != null && c.Customer.Name.Contains(search)));
            }

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalRecords);
        }
    }
}