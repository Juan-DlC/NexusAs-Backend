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
                .Include(s => s.PaymentMethodEntity)
                .Include(s => s.Credit)  // CRÍTICO: Incluir Credit para mostrar estado correcto
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
                .Include(s => s.ProcessedByUser)  // BUG 1 FIX: Incluir quien procesó la venta
                .Include(s => s.PaymentMethodEntity)
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Product)
                .Include(s => s.Credit)
                    .ThenInclude(c => c!.Installments)
                .Include(s => s.Returns)
                    .ThenInclude(r => r.ReturnDetails)
                        .ThenInclude(rd => rd.Product)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<Sale>> GetTodaySalesAsync(DateTime from, DateTime to)
        {
            return await _context.Sales
                .Include(s => s.PaymentMethodEntity)
                .Where(s => s.IsActive && s.Date >= from && s.Date < to)
                .ToListAsync();
        }

        // TAREA 2: Métodos para dashboard con PaymentMethodName incluido
        public async Task<IEnumerable<Sale>> GetSalesForDashboardAsync(int userId, string userRole, DateTime from, DateTime to)
        {
            var query = _context.Sales
                .Include(s => s.PaymentMethodEntity)
                .Where(s => s.IsActive && s.Date >= from && s.Date < to);

            if (userRole != "Admin")
                query = query.Where(s => s.UserId == userId);

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Sale>> GetRecentSalesAsync(int userId, string userRole, int count)
        {
            var query = _context.Sales
                .Include(s => s.PaymentMethodEntity)
                .Include(s => s.Customer)
                .Include(s => s.User)
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.Date)
                .Take(count);

            if (userRole != "Admin")
                query = query.Where(s => s.UserId == userId).OrderByDescending(s => s.Date).Take(count);

            return await query.ToListAsync();
        }

        // TAREA 3: OPTIMIZACIÓN - Obtener el último número de factura de forma eficiente
        public async Task<string?> GetLastSaleNumberAsync()
        {
            return await _context.Sales
                .OrderByDescending(s => s.Id)
                .Select(s => s.SaleNumber)
                .FirstOrDefaultAsync();
        }
    }
}
