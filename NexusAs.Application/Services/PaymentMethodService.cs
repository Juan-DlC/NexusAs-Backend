using AutoMapper;
using NexusAs.Application.DTOs.PaymentMethods;
using NexusAs.Application.Interfaces;

namespace NexusAs.Application.Services
{
    public class PaymentMethodService : IPaymentMethodService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PaymentMethodService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PaymentMethodDto>> GetAllActiveAsync()
        {
            var paymentMethods = await _unitOfWork.PaymentMethods
                .FindAsync(pm => pm.IsActive);

            return _mapper.Map<IEnumerable<PaymentMethodDto>>(
                paymentMethods.OrderBy(pm => pm.Name));
        }
    }
}
