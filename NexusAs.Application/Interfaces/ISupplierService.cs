using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Suppliers;

namespace NexusAs.Application.Interfaces
{
    public interface ISupplierService
    {
        Task<PagedResponseDto<SupplierDto>> GetAllAsync(
            int pageNumber, int pageSize, string? search = null);
        Task<SupplierDto?> GetByIdAsync(int id);
        Task<SupplierDto> CreateAsync(CreateSupplierDto dto);
        Task<SupplierDto> UpdateAsync(int id, UpdateSupplierDto dto);
        Task DeleteAsync(int id);
    }
}
