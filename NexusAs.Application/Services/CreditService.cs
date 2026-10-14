using AutoMapper;
using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Credits;
using NexusAs.Application.Helpers;
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

        public async Task<PagedResponseDto<CreditDto>> GetAllAsync(
            int pageNumber, int pageSize,
            string? search = null,
            string? status = null,
            int userId = 0,
            string userRole = "Admin")
        {
            var (credits, totalRecords) = await _unitOfWork.Credits.GetAllPagedAsync(
                pageNumber, pageSize, search, status, userId, userRole);

            var dtos = _mapper.Map<IEnumerable<CreditDto>>(credits);

            return new PagedResponseDto<CreditDto>
            {
                Data = dtos,
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<CreditDto?> GetByIdAsync(int id)
        {
            var credit = await _unitOfWork.Credits.GetByIdWithDetailsAsync(id);
            if (credit == null)
                throw new NotFoundException("Credit", id);
            return _mapper.Map<CreditDto>(credit);
        }

        public async Task<CreditDetailDto> GetFullDetailsAsync(int id)
        {
            var credit = await _unitOfWork.Credits.GetByIdFullDetailsAsync(id);
            if (credit == null)
                throw new NotFoundException("Credit", id);

            var dto = new CreditDetailDto
            {
                Id = credit.Id,
                SaleNumber = credit.Sale?.SaleNumber ?? "",
                CustomerName = credit.Customer?.Name ?? "",
                CustomerId = credit.CustomerId,
                TotalAmount = credit.TotalAmount,
                PaidAmount = credit.PaidAmount,
                PendingAmount = credit.PendingAmount,
                Status = credit.Status.ToString(),
                CreatedAt = credit.CreatedAt,
                Notes = credit.Notes,
                NumberOfInstallments = credit.NumberOfInstallments,
                SaleId = credit.SaleId,
                Products = credit.Sale?.SaleDetails.Select(sd => new CreditDetailProductDto
                {
                    ProductName = sd.Product?.Name ?? "",
                    Quantity = sd.Quantity,
                    UnitPrice = sd.UnitPrice,
                    Subtotal = sd.Subtotal
                }).ToList() ?? new(),
                Payments = credit.Payments.OrderByDescending(p => p.Date).Select(p => new CreditPaymentHistoryDto
                {
                    Amount = p.Amount,
                    Date = p.Date,
                    Notes = p.Notes,
                    UserName = p.User?.FullName ?? ""
                }).ToList(),
                Installments = credit.Installments.OrderBy(i => i.Number).Select(i => new CreditInstallmentDto
                {
                    Number = i.Number,
                    Amount = i.Amount,
                    IsPaid = i.IsPaid
                }).ToList()
            };

            return dto;
        }

        public async Task<CreditDto> RegisterPaymentAsync(int creditId, PaymentDto dto, int userId, string userRole = "Admin")
        {
            var credit = await _unitOfWork.Credits.GetByIdFullDetailsAsync(creditId);
            if (credit == null)
                throw new NotFoundException("Credit", creditId);
            
            // TAREA 4: Validar que el Partner solo pueda abonar a sus propios créditos
            if (userRole == "Partner")
            {
                if (credit.Sale?.UserId != userId)
                    throw new BusinessException("No está autorizado para abonar a este crédito.");
            }
            
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

            // Distribuir el abono entre las cuotas pendientes, en orden
            var remaining = dto.Amount;
            var pendingInstallments = credit.Installments
                .Where(i => !i.IsPaid)
                .OrderBy(i => i.Number)
                .ToList();

            foreach (var installment in pendingInstallments)
            {
                if (remaining <= 0) break;

                if (remaining >= installment.Amount)
                {
                    installment.IsPaid = true;
                    remaining -= installment.Amount;
                }
                else
                {
                    // Abono parcial sobre esta cuota: reduce su valor pendiente
                    installment.Amount -= remaining;
                    remaining = 0;
                }
                _unitOfWork.CreditInstallments.Update(installment);
            }

            // Registrar el abono en el historial
            var payment = new Domain.Entities.CreditPayment
            {
                CreditId = credit.Id,
                Amount = dto.Amount,
                Date = DateTimeHelper.Now,
                Notes = dto.Notes,
                UserId = userId
            };
            await _unitOfWork.CreditPayments.AddAsync(payment);

            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<CreditDto>(credit);
        }
    }
}