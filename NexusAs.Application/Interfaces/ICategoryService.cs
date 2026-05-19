using NexusAs.Application.DTOs.Categories;
using NexusAs.Application.DTOs.Common;

namespace NexusAs.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<PagedResponseDto<CategoryDto>> GetAllAsync(
            int pageNumber, int pageSize, string? search = null);
        Task<CategoryDto?> GetByIdAsync(int id);
        Task<CategoryDto> CreateAsync(CreateCategoryDto dto);
        Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto dto);
        Task DeleteAsync(int id);
    }
}