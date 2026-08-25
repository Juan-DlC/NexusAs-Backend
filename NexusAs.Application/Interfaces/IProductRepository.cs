using NexusAs.Domain.Entities;

namespace NexusAs.Application.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<(IEnumerable<Product> Items, int TotalRecords)> GetAllPagedAsync(
            string? search, int? categoryId, bool? isPartnership,
            int pageNumber, int pageSize, bool? inStock = null, bool? isActiveFilter = null);
        Task<bool> CodeExistsAsync(string code, int? excludeId = null);
        Task<Product?> GetByIdIncludingInactiveAsync(int id);
        
        // TAREA 1: Método para obtener productos con Category y Supplier incluidos
        Task<IEnumerable<Product>> GetProductsWithCategoryAsync();
        
        // TAREA 1: OPTIMIZACIÓN - Cargar múltiples productos por IDs en una sola consulta
        Task<Dictionary<int, Product>> GetProductsByIdsAsync(IEnumerable<int> productIds);
    }
}
