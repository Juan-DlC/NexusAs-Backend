using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Credits;

namespace NexusAs.Application.Interfaces
{
    public interface ICreditService
    {
        Task<PagedResponseDto<CreditDto>> GetAllAsync(
            int pageNumber, int pageSize,
            string? search = null,
            string? status = null,
            int userId = 0,
            string userRole = "Admin");
        Task<CreditDto?> GetByIdAsync(int id);
        Task<CreditDto> RegisterPaymentAsync(int creditId, PaymentDto dto, int userId, string userRole = "Admin");
        Task<CreditDetailDto> GetFullDetailsAsync(int id);
    }
}