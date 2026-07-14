using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Products;

namespace NexusAs.Application.Interfaces
{
    public interface IProductService
    {
        Task<PagedResponseDto<ProductDto>> GetAllAsync(
            int pageNumber, int pageSize, string? search = null,
            int? categoryId = null, bool? isPartnership = null,
            bool? inStock = null);
        Task<ProductDto?> GetByIdAsync(int id);
        Task<IEnumerable<ProductDto>> GetLowStockAsync();
        Task<ProductDto> CreateAsync(CreateProductDto dto);
        Task<ProductDto> UpdateAsync(int id, UpdateProductDto dto);
        Task DeleteAsync(int id);
    }
}