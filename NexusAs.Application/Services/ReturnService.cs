using AutoMapper;
using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Returns;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Entities;
using NexusAs.Domain.Enums;
using NexusAs.Domain.Exceptions;

namespace NexusAs.Application.Services
{
    public class ReturnService : IReturnService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ReturnService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<PagedResponseDto<ReturnDto>> GetAllAsync(
            int pageNumber, int pageSize, DateTime? from = null, DateTime? to = null)
        {
            var returns = await _unitOfWork.Returns.FindAsync(r =>
                r.IsActive &&
                (from == null || r.Date >= from) &&
                (to == null || r.Date <= to));

            var totalRecords = returns.Count();
            var paged = returns
                .OrderByDescending(r => r.Date)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            return new PagedResponseDto<ReturnDto>
            {
                Data = _mapper.Map<IEnumerable<ReturnDto>>(paged),
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<ReturnDto?> GetByIdAsync(int id)
        {
            var returnEntity = await _unitOfWork.Returns.GetByIdAsync(id);
            if (returnEntity == null)
                throw new NotFoundException(nameof(Return), id);
            
            return _mapper.Map<ReturnDto>(returnEntity);
        }

        public async Task<ReturnDto> CreateAsync(CreateReturnDto dto, int userId)
        {
            var sale = await GetAndValidateSaleAsync(dto.SaleId);
            await ValidateReturnDetailsAsync(dto, sale);

            var returnNumber = $"DEV-{DateTime.Now:yyyyMMddHHmmss}";
            var seller = await _unitOfWork.Users.GetByIdAsync(sale.UserId);
            var returnType = seller?.Role == UserRole.Partner 
                ? ReturnType.PartnerReturn 
                : ReturnType.CustomerReturn;

            var returnEntity = new Return
            {
                SaleId = dto.SaleId,
                Date = DateTime.Now,
                Notes = dto.Notes,
                UserId = userId,
                Type = returnType,
                ReturnDetails = dto.Details.Select(d => new ReturnDetail
                {
                    ProductId = d.ProductId,
                    Quantity = d.Quantity,
                    UnitPrice = sale.SaleDetails.First(sd => sd.ProductId == d.ProductId).UnitPrice,
                    Subtotal = d.Quantity * sale.SaleDetails.First(sd => sd.ProductId == d.ProductId).UnitPrice
                }).ToList()
            };

            returnEntity.TotalAmount = returnEntity.ReturnDetails.Sum(d => d.Subtotal);
            await _unitOfWork.Returns.AddAsync(returnEntity);

            await ProcessStockReturnAsync(returnEntity, sale, returnNumber, userId);
            UpdateCreditAfterReturnAsync(sale, returnEntity.TotalAmount);
            
            if (returnType == ReturnType.PartnerReturn)
                await UpdatePartnerSaleAfterReturnAsync(sale, returnEntity.ReturnDetails.ToList());

            sale.Total = Math.Max(0, sale.Total - returnEntity.TotalAmount);
            sale.Subtotal = Math.Max(0, sale.Subtotal - returnEntity.TotalAmount);
            
            if (sale.Total <= 0)
                sale.Status = SaleStatus.FullReturn;
            else
                sale.Status = SaleStatus.PartialReturn;
            
            _unitOfWork.Sales.Update(sale);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ReturnDto>(returnEntity);
        }

        public async Task<IEnumerable<ReturnDto>> GetBySaleIdAsync(int saleId)
        {
            var returns = await _unitOfWork.Returns.FindAsync(r =>
                r.SaleId == saleId && r.IsActive);

            return _mapper.Map<IEnumerable<ReturnDto>>(returns.OrderByDescending(r => r.Date));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // MÉTODOS PRIVADOS DE REFACTORIZACIÓN
        // ═══════════════════════════════════════════════════════════════════════

        private async Task<Sale> GetAndValidateSaleAsync(int saleId)
        {
            var sale = await _unitOfWork.Sales.GetSaleByIdWithDetailsAsync(saleId);
            if (sale == null)
                throw new NotFoundException(nameof(Sale), saleId);
            return sale;
        }

        private async Task ValidateReturnDetailsAsync(CreateReturnDto dto, Sale sale)
        {
            // Obtener TODAS las devoluciones previas de esta venta
            var previousReturns = await _unitOfWork.Returns.FindAsync(r => 
                r.SaleId == sale.Id && r.IsActive);
            
            foreach (var detail in dto.Details)
            {
                var saleDetail = sale.SaleDetails.FirstOrDefault(sd => sd.ProductId == detail.ProductId);
                if (saleDetail == null)
                    throw new BusinessException(
                        $"El producto {detail.ProductId} no pertenece a esta venta.");

                // Calcular cuánto se ha devuelto previamente de este producto
                var totalPreviouslyReturned = 0;
                foreach (var prevReturn in previousReturns)
                {
                    var prevReturnDetails = await _unitOfWork.ReturnDetails.FindAsync(rd => 
                        rd.ReturnId == prevReturn.Id && rd.ProductId == detail.ProductId);
                    totalPreviouslyReturned += prevReturnDetails.Sum(rd => rd.Quantity);
                }

                // Calcular cantidad disponible para devolución
                var availableToReturn = saleDetail.Quantity - totalPreviouslyReturned;

                if (detail.Quantity > availableToReturn)
                    throw new BusinessException(
                        $"La cantidad a devolver ({detail.Quantity}) excede la disponible para devolución. " +
                        $"Vendido originalmente: {saleDetail.Quantity}, " +
                        $"Ya devuelto: {totalPreviouslyReturned}, " +
                        $"Disponible para devolver: {availableToReturn}.");
            }
        }

        private async Task ProcessStockReturnAsync(Return returnEntity, Sale sale, string returnNumber, int userId)
        {
            foreach (var returnDetail in returnEntity.ReturnDetails)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(returnDetail.ProductId);
                if (product == null) continue;

                var stockBefore = product.Stock;
                product.Stock += returnDetail.Quantity;
                _unitOfWork.Products.Update(product);

                var movement = new StockMovement
                {
                    ProductId = returnDetail.ProductId,
                    Date = DateTime.Now,
                    Type = MovementType.Entry,
                    Quantity = returnDetail.Quantity,
                    StockBefore = stockBefore,
                    StockAfter = product.Stock,
                    Reason = $"Devolución {returnNumber}",
                    SaleId = sale.Id,
                    UserId = userId
                };
                await _unitOfWork.StockMovements.AddAsync(movement);
            }
        }

        private void UpdateCreditAfterReturnAsync(Sale sale, decimal returnedAmount)
        {
            if (sale.Credit == null) return;

            var credit = sale.Credit;
            credit.TotalAmount = Math.Max(0, credit.TotalAmount - returnedAmount);
            credit.PendingAmount = Math.Max(0, credit.TotalAmount - credit.PaidAmount);
            
            credit.Status = credit.PendingAmount <= 0 ? CreditStatus.Paid :
                           credit.PaidAmount > 0 ? CreditStatus.Partial : CreditStatus.Pending;

            var pendingInstallments = credit.Installments?
                .Where(i => !i.IsPaid)
                .OrderBy(i => i.Number)
                .ToList() ?? new List<CreditInstallment>();

            var remaining = returnedAmount;
            foreach (var inst in pendingInstallments)
            {
                if (remaining <= 0) break;
                
                if (remaining >= inst.Amount)
                {
                    inst.IsPaid = true;
                    remaining -= inst.Amount;
                }
                else
                {
                    inst.Amount -= remaining;
                    remaining = 0;
                }
            }

            _unitOfWork.Credits.Update(credit);
        }

        private async Task UpdatePartnerSaleAfterReturnAsync(Sale sale, List<ReturnDetail> returnDetails)
        {
            foreach (var returnDetail in returnDetails)
            {
                var partnerSales = await _unitOfWork.PartnerSales.FindAsync(ps =>
                    ps.SaleId == sale.Id &&
                    ps.ProductId == returnDetail.ProductId &&
                    ps.IsActive);

                foreach (var partnerSale in partnerSales)
                {
                    if (partnerSale == null || partnerSale.Quantity <= 0) continue;

                    var ratio = returnDetail.Quantity / (decimal)partnerSale.Quantity;
                    partnerSale.Quantity -= returnDetail.Quantity;
                    partnerSale.PartnerEarning -= partnerSale.PartnerEarning * ratio;
                    partnerSale.AsEarning -= partnerSale.AsEarning * ratio;

                    if (partnerSale.Quantity <= 0)
                        partnerSale.IsActive = false;

                    _unitOfWork.PartnerSales.Update(partnerSale);
                }
            }
        }
    }
}
