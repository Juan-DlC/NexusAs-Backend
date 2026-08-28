using Microsoft.EntityFrameworkCore;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Entities;
using NexusAs.Infrastructure.Data;

namespace NexusAs.Infrastructure.Repositories
{
    public class BusinessPartnerRepository : BaseRepository<BusinessPartner>, IBusinessPartnerRepository
    {
        public BusinessPartnerRepository(NexusAsDbContext context) : base(context)
        {
        }

        public async Task<(IEnumerable<BusinessPartner> data, int total)> GetAllPagedWithProductsAsync(
            int pageNumber,
            int pageSize,
            string? search,
            bool? isActiveFilter)
        {
            var query = _context.BusinessPartners
                .Include(bp => bp.Products.Where(p => p.IsActive))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(bp =>
                    bp.Name.Contains(search) ||
                    bp.DocumentNumber.Contains(search));
            }

            if (isActiveFilter.HasValue)
            {
                query = query.Where(bp => bp.IsActive == isActiveFilter.Value);
            }
            else
            {
                query = query.Where(bp => bp.IsActive);
            }

            var totalRecords = await query.CountAsync();
            var businessPartners = await query
                .OrderByDescending(bp => bp.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (businessPartners, totalRecords);
        }

        public async Task<BusinessPartner?> GetByIdWithProductsAsync(int id)
        {
            return await _context.BusinessPartners
                .Include(bp => bp.Products.Where(p => p.IsActive))
                .FirstOrDefaultAsync(bp => bp.Id == id);
        }

        public async Task<List<SaleDetail>> GetSaleDetailsByBusinessPartnerAndDateRangeAsync(
            int businessPartnerId,
            DateTime fromDate,
            DateTime toDate)
        {
            // BUG FIX: Incluir Credit y PaymentMethodEntity para determinar si está pagada
            return await _context.SaleDetails
                .Include(sd => sd.Sale)
                    .ThenInclude(s => s.User)
                .Include(sd => sd.Sale)
                    .ThenInclude(s => s.Credit)
                .Include(sd => sd.Sale)
                    .ThenInclude(s => s.PaymentMethodEntity)
                .Include(sd => sd.Product)
                    .ThenInclude(p => p.BusinessPartner)
                .Where(sd =>
                    sd.Product.BusinessPartnerId == businessPartnerId &&
                    sd.Sale.Date >= fromDate &&
                    sd.Sale.Date <= toDate &&
                    sd.Sale.IsActive &&
                    sd.IsActive &&
                    // BUG FIX: La venta está completamente pagada si:
                    // - No tiene crédito (es contado) O
                    // - Tiene crédito y está pagado
                    (sd.Sale.Credit == null || sd.Sale.Credit.Status == Domain.Enums.CreditStatus.Paid) &&
                    // EXCLUIR ventas ya liquidadas para este socio comercial
                    !_context.BusinessPartnerLiquidationDetails
                        .Any(bpld => bpld.SaleId == sd.SaleId && 
                                   bpld.Liquidation.BusinessPartnerId == businessPartnerId && 
                                   bpld.Liquidation.IsActive))
                .ToListAsync();
        }

        public async Task<List<int>> GetAlreadyLiquidatedSaleIdsAsync(List<int> saleIds)
        {
            return await _context.BusinessPartnerLiquidationDetails
                .Include(d => d.Liquidation)
                .Where(d =>
                    saleIds.Contains(d.SaleId) &&
                    d.Liquidation.IsActive)
                .Select(d => d.SaleId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<string?> GetLastLiquidationNumberAsync()
        {
            return await _context.BusinessPartnerLiquidations
                .OrderByDescending(l => l.Id)
                .Select(l => l.LiquidationNumber)
                .FirstOrDefaultAsync();
        }

        public async Task<(IEnumerable<BusinessPartnerLiquidation> data, int total)> GetLiquidationsPagedAsync(
            int? businessPartnerId,
            DateTime? fromDate,
            DateTime? toDate,
            int pageNumber,
            int pageSize)
        {
            var query = _context.BusinessPartnerLiquidations
                .Include(l => l.BusinessPartner)
                .Include(l => l.ProcessedByUser)
                .Include(l => l.Details)
                    .ThenInclude(d => d.Sale)
                        .ThenInclude(s => s.User)
                .Include(l => l.Details)
                    .ThenInclude(d => d.Sale)
                        .ThenInclude(s => s.PaymentMethodEntity)
                .Include(l => l.Details)
                    .ThenInclude(d => d.Product)
                .Where(l => l.IsActive)
                .AsQueryable();

            if (businessPartnerId.HasValue)
            {
                query = query.Where(l => l.BusinessPartnerId == businessPartnerId.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(l => l.LiquidationDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(l => l.LiquidationDate <= toDate.Value);
            }

            var totalRecords = await query.CountAsync();
            var liquidations = await query
                .OrderByDescending(l => l.LiquidationDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (liquidations, totalRecords);
        }

        public async Task<BusinessPartnerLiquidation?> GetLiquidationByIdWithDetailsAsync(int id)
        {
            return await _context.BusinessPartnerLiquidations
                .Include(l => l.BusinessPartner)
                .Include(l => l.ProcessedByUser)
                .Include(l => l.Details)
                    .ThenInclude(d => d.Sale)
                        .ThenInclude(s => s.User)
                .Include(l => l.Details)
                    .ThenInclude(d => d.Sale)
                        .ThenInclude(s => s.PaymentMethodEntity)
                .Include(l => l.Details)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(l => l.Id == id);
        }
    }
}
