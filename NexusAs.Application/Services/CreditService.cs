using AutoMapper;
using NexusAs.Application.DTOs.Credits;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Enums;
using NexusAs.Domain.Exceptions;

namespace NexusAs.Application.Services
{
    public class CreditService : ICreditService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreditService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CreditDto>> GetAllAsync(string? status = null)
        {
            var credits = await _unitOfWork.Credits.GetAllWithDetailsAsync(status);
            return _mapper.Map<IEnumerable<CreditDto>>(credits);
        }

        public async Task<CreditDto?> GetByIdAsync(int id)
        {
            var credit = await _unitOfWork.Credits.GetByIdWithDetailsAsync(id);
            if (credit == null)
                throw new NotFoundException("Credit", id);
            return _mapper.Map<CreditDto>(credit);
        }

        public async Task<CreditDto> RegisterPaymentAsync(int creditId, PaymentDto dto)
        {
            var credit = await _unitOfWork.Credits.GetByIdWithDetailsAsync(creditId);
            if (credit == null)
                throw new NotFoundException("Credit", creditId);

            if (credit.Status == CreditStatus.Paid)
                throw new BusinessException("Este crédito ya está pagado.");

            if (dto.Amount <= 0)
                throw new BusinessException("El monto del abono debe ser mayor a 0.");

            if (dto.Amount > credit.PendingAmount)
                throw new BusinessException(
                    $"El abono ({dto.Amount:C}) supera el monto pendiente ({credit.PendingAmount:C}).");

            credit.PaidAmount += dto.Amount;
            credit.PendingAmount -= dto.Amount;
            credit.Status = credit.PendingAmount == 0
                ? CreditStatus.Paid
                : CreditStatus.Partial;

            _unitOfWork.Credits.Update(credit);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<CreditDto>(credit);
        }
    }
}