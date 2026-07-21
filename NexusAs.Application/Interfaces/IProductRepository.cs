using NexusAs.Domain.Entities;

namespace NexusAs.Application.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<(IEnumerable<Product> Items, int TotalRecords)> GetAllPagedAsync(
            string? search, int? categoryId, bool? isPartnership,
            int pageNumber, int pageSize, bool? inStock = null, bool includeInactive = false);
        Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    }
}