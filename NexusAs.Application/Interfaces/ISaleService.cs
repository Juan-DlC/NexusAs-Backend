using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Sales;

namespace NexusAs.Application.Interfaces
{
    public interface ISaleService
    {
        Task<PagedResponseDto<SaleDto>> GetAllAsync(
            int userId, string userRole,
            DateTime? from = null, DateTime? to = null,
            string? search = null, int pageNumber = 1, int pageSize = 10);
        Task<SaleDto?> GetByIdAsync(int id);
        Task<SaleDto> CreateAsync(CreateSaleDto dto, int userId);
    }
}