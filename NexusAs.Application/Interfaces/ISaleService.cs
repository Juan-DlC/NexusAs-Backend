using NexusAs.Application.DTOs.Sales;

namespace NexusAs.Application.Interfaces
{
    public interface ISaleService
    {
        Task<IEnumerable<SaleDto>> GetAllAsync(int userId, string userRole,
            DateTime? from = null, DateTime? to = null);
        Task<SaleDto?> GetByIdAsync(int id);
        Task<SaleDto> CreateAsync(CreateSaleDto dto, int userId);
    }
}