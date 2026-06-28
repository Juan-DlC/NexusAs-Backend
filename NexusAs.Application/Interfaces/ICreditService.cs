using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Credits;

namespace NexusAs.Application.Interfaces
{
    public interface ICreditService
    {
        Task<PagedResponseDto<CreditDto>> GetAllAsync(
            string? status = null, string? search = null,
            int pageNumber = 1, int pageSize = 10);
        Task<CreditDto?> GetByIdAsync(int id);
        Task<CreditDto> RegisterPaymentAsync(int creditId, PaymentDto dto, int userId);
        Task<CreditDetailDto> GetFullDetailsAsync(int id);
    }
}