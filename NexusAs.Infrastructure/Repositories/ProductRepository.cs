using Microsoft.EntityFrameworkCore;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Entities;
using NexusAs.Infrastructure.Data;

namespace NexusAs.Infrastructure.Repositories
{
    public class ProductRepository : BaseRepository<Product>, IProductRepository
    {
        public ProductRepository(NexusAsDbContext context) : base(context)
        {
        }

        public async Task<(IEnumerable<Product> Items, int TotalRecords)> GetAllPagedAsync(
            string? search, int? categoryId, bool? isPartnership,
            int pageNumber, int pageSize, bool? inStock = null, bool includeInactive = false)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => includeInactive || p.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.Code.Contains(search) ||
                    (p.Description != null && p.Description.Contains(search)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (isPartnership.HasValue)
            {
                query = query.Where(p => p.IsPartnership == isPartnership.Value);
            }

            if (inStock == true)
            {
                query = query.Where(p => p.Stock > 0);
            }

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderBy(p => p.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalRecords);
        }

        public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
        {
            return await _context.Products.AnyAsync(p =>
                p.Code == code && p.IsActive &&
                (excludeId == null || p.Id != excludeId.Value));
        }
    }
}