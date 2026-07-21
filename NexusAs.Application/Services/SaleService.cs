using AutoMapper;
using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Sales;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Entities;
using NexusAs.Domain.Enums;
using NexusAs.Domain.Exceptions;
namespace NexusAs.Application.Services
{
    public class SaleService : ISaleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public SaleService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<PagedResponseDto<SaleDto>> GetAllAsync(
            int userId, string userRole,
            DateTime? from = null, DateTime? to = null,
            string? search = null, int pageNumber = 1, int pageSize = 10)
        {
            var (sales, totalRecords) = await _unitOfWork.Sales.GetSalesWithDetailsAsync(
                userId, userRole, from, to, search, pageNumber, pageSize);
            var dtos = _mapper.Map<IEnumerable<SaleDto>>(sales);
            return new PagedResponseDto<SaleDto>
            {
                Data = dtos,
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        public async Task<SaleDto?> GetByIdAsync(int id)
        {
            var sale = await _unitOfWork.Sales.GetSaleByIdWithDetailsAsync(id);
            if (sale == null)
                throw new NotFoundException(nameof(Sale), id);
            var dto = _mapper.Map<SaleDto>(sale);
            if (sale.Credit != null)
            {
                dto.CreditInfo = new CreditInfoDto
                {
                    TotalAmount = sale.Credit.TotalAmount,
                    PaidAmount = sale.Credit.PaidAmount,
                    PendingAmount = sale.Credit.PendingAmount,
                    Status = sale.Credit.Status.ToString(),
                    NumberOfInstallments = sale.Credit.NumberOfInstallments,
                    Installments = sale.Credit.Installments
                        .OrderBy(i => i.Number)
                        .Select(i => new InstallmentInfoDto
                        {
                            Number = i.Number,
                            Amount = i.Amount,
                            IsPaid = i.IsPaid
                        }).ToList()
                };
            }
            return dto;
        }
        public async Task<SaleDto> CreateAsync(CreateSaleDto dto, int userId)
        {
            //Validar stock de todos los productos antes de crear la venta
            foreach (var detail in dto.Details)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(detail.ProductId);
                if (product == null)
                    throw new NotFoundException(nameof(Product), detail.ProductId);
                if (product.Stock < detail.Quantity)
                    throw new BusinessException(
                        $"Stock insuficiente para '{product.Name}'. " +
                        $"Disponible: {product.Stock}, Solicitado: {detail.Quantity}.");
            }
            //Generar número de factura
            var salesCount = (await _unitOfWork.Sales.GetAllAsync()).Count();
            var saleNumber = $"FAC-{(salesCount + 1):D4}";
            //Calcular totales
            var subtotal = dto.Details.Sum(d => d.Quantity * d.UnitPrice);
            var total = subtotal - dto.Discount;
            //Crear la venta
            // Buscar el método de pago por código
            var paymentMethods = await _unitOfWork.PaymentMethods.GetAllAsync();
            var paymentMethod = paymentMethods.FirstOrDefault(pm =>
                pm.Code.Equals(dto.PaymentMethod, StringComparison.OrdinalIgnoreCase));

            if (paymentMethod == null)
                throw new BusinessException($"Método de pago '{dto.PaymentMethod}' no válido.");

            var sale = new Sale
            {
                SaleNumber = saleNumber,
                Date = DateTime.Now,
                Subtotal = subtotal,
                Discount = dto.Discount,
                Total = total,
                PaymentMethodId = paymentMethod.Id,
                Notes = dto.Notes,
                CustomerId = dto.CustomerId,
                UserId = userId
            };
            await _unitOfWork.Sales.AddAsync(sale);
            await _unitOfWork.SaveChangesAsync();
            //Crear detalles, descontar stock y registrar movimientos
            foreach (var detailDto in dto.Details)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(detailDto.ProductId);
                //Crear detalle de venta
                var saleDetail = new SaleDetail
                {
                    SaleId = sale.Id,
                    ProductId = detailDto.ProductId,
                    Quantity = detailDto.Quantity,
                    UnitPrice = detailDto.UnitPrice,
                    Subtotal = detailDto.Quantity * detailDto.UnitPrice
                };
                await _unitOfWork.SaleDetails.AddAsync(saleDetail);
                // Registrar movimiento de stock
                var stockBefore = product!.Stock;
                var movement = new StockMovement
                {
                    ProductId = detailDto.ProductId,
                    Date = DateTime.Now,
                    Type = MovementType.Exit,
                    Quantity = -detailDto.Quantity,
                    StockBefore = stockBefore,
                    StockAfter = stockBefore - detailDto.Quantity,
                    Reason = $"Venta {saleNumber}",
                    SaleId = sale.Id,
                    UserId = userId
                };
                await _unitOfWork.StockMovements.AddAsync(movement);
                // Descontar stock
                product.Stock -= detailDto.Quantity;
                _unitOfWork.Products.Update(product);
            }
            //Si es crédito, crear registro de crédito
            if (paymentMethod.Code == "CREDIT")
            {
                if (dto.CustomerId == null)
                    throw new BusinessException(
                        "Las ventas a crédito requieren un cliente registrado.");
                var numberOfInstallments = dto.NumberOfInstallments ?? 1;
                if (numberOfInstallments < 1)
                    numberOfInstallments = 1;
                var credit = new Credit
                {
                    SaleId = sale.Id,
                    CustomerId = dto.CustomerId.Value,
                    TotalAmount = total,
                    PaidAmount = 0,
                    PendingAmount = total,
                    Status = CreditStatus.Pending,
                    NumberOfInstallments = numberOfInstallments
                };
                await _unitOfWork.Credits.AddAsync(credit);
                await _unitOfWork.SaveChangesAsync();
                // Generar las cuotas automáticamente
                var installmentAmount = Math.Round(total / numberOfInstallments, 2);
                for (int i = 1; i <= numberOfInstallments; i++)
                {
                    var installment = new CreditInstallment
                    {
                        CreditId = credit.Id,
                        Number = i,
                        Amount = installmentAmount,
                        IsPaid = false
                    };
                    await _unitOfWork.CreditInstallments.AddAsync(installment);
                }
            }
            // Si el vendedor es Partner, registrar ganancias
            var seller = await _unitOfWork.Users.GetByIdAsync(userId);
            if (seller?.Role == UserRole.Partner)
            {
                var partnerConfig = (await _unitOfWork.PartnerConfigs
                    .FindAsync(pc => pc.UserId == userId && pc.IsActive))
                    .FirstOrDefault();
                if (partnerConfig != null)
                {
                    foreach (var detailDto in dto.Details)
                    {
                        var product = await _unitOfWork.Products
                            .GetByIdAsync(detailDto.ProductId);
                        decimal partnerPrice;
                        decimal commissionPercent;
                        if (product!.IsPartnership)
                        {
                            // Producto de alianza: se entrega al precio de venta completo, sin descuento
                            partnerPrice = product.SalePrice;
                            commissionPercent = 0;
                        }
                        else
                        {
                            // Producto normal: precio = costo + (ganancia AS * % comisión / 100)
                            commissionPercent = partnerConfig.CommissionPercent;
                            var gainAS = product.SalePrice - product.Cost;
                            partnerPrice = product.Cost + (gainAS * commissionPercent / 100);
                        }
                        var partnerEarning = Math.Max(0, detailDto.UnitPrice - partnerPrice);
                        var asEarning = partnerPrice - product.Cost;
                        var partnerSale = new PartnerSale
                        {
                            PartnerConfigId = partnerConfig.Id,
                            SaleId = sale.Id,
                            ProductId = detailDto.ProductId,
                            Quantity = detailDto.Quantity,
                            CostPrice = product.Cost,
                            PartnerPrice = partnerPrice,
                            SalePrice = detailDto.UnitPrice,
                            CommissionPercent = commissionPercent,
                            PartnerEarning = partnerEarning * detailDto.Quantity,
                            AsEarning = asEarning * detailDto.Quantity,
                            IsPartnership = product.IsPartnership,
                            Date = DateTime.Now
                        };
                        await _unitOfWork.PartnerSales.AddAsync(partnerSale);
                    }
                }
            }
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<SaleDto>(sale);
        }
    }
}