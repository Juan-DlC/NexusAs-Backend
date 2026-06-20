using AutoMapper;
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

        public async Task<IEnumerable<SaleDto>> GetAllAsync(
            int userId, string userRole,
            DateTime? from = null, DateTime? to = null)
        {
            var sales = await _unitOfWork.Sales.GetSalesWithDetailsAsync(userId, userRole, from, to);
            return _mapper.Map<IEnumerable<SaleDto>>(sales);
        }

        public async Task<SaleDto?> GetByIdAsync(int id)
        {
            var sale = await _unitOfWork.Sales.GetSaleByIdWithDetailsAsync(id);
            if (sale == null)
                throw new NotFoundException(nameof(Sale), id);
            return _mapper.Map<SaleDto>(sale);
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
            var paymentMethod = dto.PaymentMethod == "Cash"
                ? PaymentMethod.Cash
                : PaymentMethod.Credit;

            var sale = new Sale
            {
                SaleNumber = saleNumber,
                Date = DateTime.Now,
                Subtotal = subtotal,
                Discount = dto.Discount,
                Total = total,
                PaymentMethod = paymentMethod,
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
            if (paymentMethod == PaymentMethod.Credit)
            {
                if (dto.CustomerId == null)
                    throw new BusinessException(
                        "Las ventas a crédito requieren un cliente registrado.");

                var credit = new Credit
                {
                    SaleId = sale.Id,
                    CustomerId = dto.CustomerId.Value,
                    TotalAmount = total,
                    PaidAmount = 0,
                    PendingAmount = total,
                    Status = CreditStatus.Pending
                };
                await _unitOfWork.Credits.AddAsync(credit);
            }

            // 7. Si el vendedor es Partner, registrar ganancias
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

                        // Determinar porcentaje de comisión
                        decimal commissionPercent = partnerConfig.CommissionPercent;

                        if (product!.IsPartnership)
                        {
                            var customPrice = (await _unitOfWork.PartnerProductPrices
                                .FindAsync(pp =>
                                    pp.PartnerConfigId == partnerConfig.Id &&
                                    pp.ProductId == detailDto.ProductId &&
                                    pp.IsActive))
                                .FirstOrDefault();

                            if (customPrice != null)
                                commissionPercent = customPrice.CustomCommission;
                        }

                        // Calcular precio de socia = costo + (gananciaAS * commissionPercent/100)
                        var gainAS = product.SalePrice - product.Cost;
                        var partnerPrice = product.Cost + (gainAS * commissionPercent / 100);
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