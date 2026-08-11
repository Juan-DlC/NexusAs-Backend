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
            // 1. Obtener la venta con sus detalles
            var sale = await _unitOfWork.Sales.GetSaleByIdWithDetailsAsync(dto.SaleId);
            if (sale == null)
                throw new NotFoundException(nameof(Sale), dto.SaleId);

            // 2. Validar que los productos devueltos pertenezcan a la venta
            foreach (var detail in dto.Details)
            {
                var saleDetail = sale.SaleDetails.FirstOrDefault(sd => sd.ProductId == detail.ProductId);
                if (saleDetail == null)
                    throw new BusinessException(
                        $"El producto ID {detail.ProductId} no pertenece a esta venta.");

                if (detail.Quantity > saleDetail.Quantity)
                    throw new BusinessException(
                        $"La cantidad a devolver ({detail.Quantity}) excede la cantidad vendida ({saleDetail.Quantity}).");
            }

            // 3. Determinar el tipo de devolución (Cliente o Socia)
            var seller = await _unitOfWork.Users.GetByIdAsync(sale.UserId);
            var returnType = seller?.Role == UserRole.Partner 
                ? ReturnType.PartnerReturn 
                : ReturnType.CustomerReturn;

            // 4. Crear la devolución
            decimal totalAmount = 0;
            var returnEntity = new Return
            {
                SaleId = dto.SaleId,
                Date = DateTime.Now,
                Notes = dto.Notes,
                UserId = userId,
                Type = returnType
            };

            await _unitOfWork.Returns.AddAsync(returnEntity);
            await _unitOfWork.SaveChangesAsync();

            // 5. Crear detalles de la devolución y procesar cada producto
            foreach (var detailDto in dto.Details)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(detailDto.ProductId);
                var saleDetail = sale.SaleDetails.First(sd => sd.ProductId == detailDto.ProductId);

                // Crear detalle de devolución
                var returnDetail = new ReturnDetail
                {
                    ReturnId = returnEntity.Id,
                    ProductId = detailDto.ProductId,
                    Quantity = detailDto.Quantity,
                    UnitPrice = saleDetail.UnitPrice,
                    Subtotal = detailDto.Quantity * saleDetail.UnitPrice
                };
                await _unitOfWork.ReturnDetails.AddAsync(returnDetail);
                totalAmount += returnDetail.Subtotal;

                // 6. REINGRESO DE STOCK
                var stockBefore = product!.Stock;
                product.Stock += detailDto.Quantity;
                _unitOfWork.Products.Update(product);

                // Registrar movimiento de stock
                var stockMovement = new StockMovement
                {
                    ProductId = detailDto.ProductId,
                    Date = DateTime.Now,
                    Type = MovementType.Entry,
                    Quantity = detailDto.Quantity,
                    StockBefore = stockBefore,
                    StockAfter = product.Stock,
                    Reason = $"Devolución - Venta {sale.SaleNumber}",
                    SaleId = sale.Id,
                    UserId = userId
                };
                await _unitOfWork.StockMovements.AddAsync(stockMovement);

                // 6.1 ACTUALIZAR SALEDETAIL
                saleDetail.Quantity -= detailDto.Quantity;
                if (saleDetail.Quantity <= 0)
                {
                    saleDetail.IsActive = false;
                    saleDetail.Quantity = 0; // Asegurar que quede en 0, no negativo
                }
                saleDetail.Subtotal = saleDetail.Quantity * saleDetail.UnitPrice;
                _unitOfWork.SaleDetails.Update(saleDetail);

                // 7. DESCUENTO DE GANANCIAS DE SOCIA (si fue venta de Partner)
                if (returnType == ReturnType.PartnerReturn)
                {
                    var partnerSales = await _unitOfWork.PartnerSales.FindAsync(ps =>
                        ps.SaleId == sale.Id && 
                        ps.ProductId == detailDto.ProductId &&
                        ps.IsActive);

                    foreach (var partnerSale in partnerSales)
                    {
                        if (partnerSale == null) continue; // Null check

                        // Calcular proporción a descontar
                        decimal quantityReturned = detailDto.Quantity;
                        decimal quantitySold = partnerSale.Quantity;
                        
                        if (quantitySold <= 0) continue; // Evitar división por cero
                        
                        decimal returnRatio = Math.Min(quantityReturned / quantitySold, 1);

                        // Descontar ganancias proporcionalmente
                        partnerSale.PartnerEarning -= partnerSale.PartnerEarning * returnRatio;
                        partnerSale.AsEarning -= partnerSale.AsEarning * returnRatio;
                        partnerSale.Quantity -= (int)quantityReturned;

                        if (partnerSale.Quantity <= 0)
                        {
                            // Marcar como inactivo en vez de eliminar (auditoría)
                            partnerSale.IsActive = false;
                            partnerSale.Quantity = 0;
                            _unitOfWork.PartnerSales.Update(partnerSale);
                        }
                        else
                        {
                            _unitOfWork.PartnerSales.Update(partnerSale);
                        }
                    }
                }
            }

            // Actualizar total de la devolución
            returnEntity.TotalAmount = totalAmount;
            _unitOfWork.Returns.Update(returnEntity);

            // 8.1 RECALCULAR SALE.SUBTOTAL Y SALE.TOTAL (TAREA 3.5)
            var activeDetails = sale.SaleDetails.Where(sd => sd.IsActive).ToList();
            sale.Subtotal = activeDetails.Sum(sd => sd.Subtotal);
            sale.Total = sale.Subtotal - sale.Discount;

            // 8.2 RECALCULAR CRÉDITO SI EXISTE (TAREA 3.1 y 3.2)
            if (sale.Credit != null)
            {
                var credit = await _unitOfWork.Credits.GetByIdWithDetailsAsync(sale.Credit.Id);
                if (credit == null)
                {
                    // TAREA 3.1: Si credit es null, solo actualizar la venta y salir
                    _unitOfWork.Sales.Update(sale);
                    await _unitOfWork.SaveChangesAsync();
                    return _mapper.Map<ReturnDto>(returnEntity);
                }

                credit.TotalAmount = sale.Total;
                credit.PendingAmount = Math.Max(0, credit.TotalAmount - credit.PaidAmount);

                // Actualizar estado del crédito
                if (credit.PendingAmount == 0)
                    credit.Status = CreditStatus.Paid;
                else if (credit.PaidAmount > 0 && credit.PaidAmount < credit.TotalAmount)
                    credit.Status = CreditStatus.Partial;
                else if (credit.PaidAmount == 0)
                    credit.Status = CreditStatus.Pending;

                _unitOfWork.Credits.Update(credit);

                // TAREA 3.2: Redistribuir cuotas pendientes con null checks robustos
                var unpaidInstallments = credit.Installments?
                    .Where(i => !i.IsPaid)
                    .OrderBy(i => i.Number)
                    .ToList() ?? new List<CreditInstallment>();

                if (unpaidInstallments.Any() && credit.PendingAmount > 0)
                {
                    decimal newInstallmentAmount = Math.Round(
                        credit.PendingAmount / unpaidInstallments.Count, 2);

                    foreach (var installment in unpaidInstallments)
                    {
                        if (installment != null)
                        {
                            installment.Amount = newInstallmentAmount;
                        }
                    }
                }
            }

            // 9. ACTUALIZAR ESTADO DE LA VENTA
            if (sale.Total <= 0)
            {
                sale.Status = SaleStatus.FullReturn;
                sale.IsActive = false;
            }
            else
            {
                // Verificar si todos los productos fueron devueltos completamente
                var allReturns = await _unitOfWork.ReturnDetails.FindAsync(rd => 
                    rd.Return!.SaleId == sale.Id && rd.Return.IsActive);
                
                bool allFullyReturned = true;
                foreach (var saleDetail in sale.SaleDetails.Where(sd => sd.IsActive))
                {
                    var totalReturned = allReturns
                        .Where(rd => rd.ProductId == saleDetail.ProductId)
                        .Sum(rd => rd.Quantity);
                    
                    if (totalReturned < saleDetail.Quantity)
                    {
                        allFullyReturned = false;
                        break;
                    }
                }

                if (allFullyReturned && !activeDetails.Any())
                {
                    sale.Status = SaleStatus.FullReturn;
                    sale.IsActive = false;
                }
                else
                {
                    sale.Status = SaleStatus.PartialReturn;
                }
            }
            
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
    }
}
