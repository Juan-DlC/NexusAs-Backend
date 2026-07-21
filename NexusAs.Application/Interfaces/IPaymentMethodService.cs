using NexusAs.Application.DTOs.PaymentMethods;

namespace NexusAs.Application.Interfaces
{
    public interface IPaymentMethodService
    {
        Task<IEnumerable<PaymentMethodDto>> GetAllActiveAsync();
    }
}
