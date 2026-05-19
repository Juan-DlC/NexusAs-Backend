using NexusAs.Application.DTOs.Credits;

namespace NexusAs.Application.Interfaces
{
    public interface ICreditService
    {
        Task<IEnumerable<CreditDto>> GetAllAsync(string? status = null);
        Task<CreditDto?> GetByIdAsync(int id);
        Task<CreditDto> RegisterPaymentAsync(int creditId, PaymentDto dto);
    }
}