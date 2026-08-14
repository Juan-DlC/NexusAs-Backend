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

            // TAREA 2: Calcular automáticamente precio de socia si UnitPrice es 0
            // BUG 1 FIX: Fórmula correcta partnerPrice = SalePrice - (gainAS × commissionPercent / 100)
            if (dto.PartnerUserId.HasValue)
            {
                var partnerConfig = (await _unitOfWork.PartnerConfigs
                    .FindAsync(pc => pc.UserId == dto.PartnerUserId.Value && pc.IsActive))
                    .FirstOrDefault();

                if (partnerConfig != null)
                {
                    foreach (var detailDto in dto.Details)
                    {
                        if (detailDto.UnitPrice <= 0)
                        {
                            var product = await _unitOfWork.Products.GetByIdAsync(detailDto.ProductId);
                            if (product != null)
                            {
                                var gainAS = product.SalePrice - product.Cost;
                                var commissionPercent = product.IsPartnership
                                    ? partnerConfig.AllianceCommissionPercent
                                    : partnerConfig.CommissionPercent;
                                
                                // BUG 1 FIX: Precio para socia = PrecioVenta - (Ganancia × Comisión%)
                                detailDto.UnitPrice = product.SalePrice - (gainAS * commissionPercent / 100);
                            }
                        }
                    }
                }
            }

            // Determinar el UserId final (socia o usuario actual)
            int finalUserId = dto.PartnerUserId ?? userId;

            // PASO 4: Validar que el userId existe en la tabla Users
            var userExists = await _unitOfWork.Users.GetByIdAsync(finalUserId);
            if (userExists == null)
            {
                throw new BusinessException($"El usuario con Id {finalUserId} no existe en el sistema. Verifique que la socia esté correctamente registrada.");
            }

            // TAREA 1: Validación explícita de PaymentMethodId ANTES de crear la venta
            if (dto.PaymentMethodId <= 0)
                throw new BusinessException("Debe seleccionar un método de pago válido.");

            var paymentMethod = await _unitOfWork.PaymentMethods.GetByIdAsync(dto.PaymentMethodId);
            if (paymentMethod == null)
                throw new BusinessException($"El método de pago seleccionado no existe.");

            // Generar número de factura - FIX: usar MAX en lugar de COUNT para evitar duplicados
            var allSales = await _unitOfWork.Sales.FindAsync(s => s.SaleNumber.StartsWith("FAC-"));
            int nextNumber = 1;
            if (allSales.Any())
            {
                var maxNumber = allSales
                    .Select(s => {
                        var numberPart = s.SaleNumber.Replace("FAC-", "");
                        return int.TryParse(numberPart, out int num) ? num : 0;
                    })
                    .DefaultIfEmpty(0)
                    .Max();
                nextNumber = maxNumber + 1;
            }
            var saleNumber = $"FAC-{nextNumber:D4}";
            
            //Calcular totales
            var subtotal = dto.Details.Sum(d => d.Quantity * d.UnitPrice);
            
            // BUG 4 FIX: Calcular descuento por porcentaje o monto fijo
            decimal discount = 0;
            decimal? discountPercent = null;
            
            if (dto.DiscountPercent.HasValue && dto.DiscountPercent > 0)
            {
                discountPercent = dto.DiscountPercent.Value;
                discount = subtotal * dto.DiscountPercent.Value / 100;
            }
            else if (dto.DiscountAmount.HasValue)
            {
                discount = dto.DiscountAmount.Value;
            }
            else if (dto.Discount > 0)  // Compatibilidad con versión anterior
            {
                discount = dto.Discount;
            }
            
            var total = subtotal - discount;

            //Crear la venta
            var sale = new Sale
            {
                SaleNumber = saleNumber,
                Date = DateTime.Now,
                Subtotal = subtotal,
                Discount = discount,
                DiscountPercent = discountPercent,
                Total = total,
                PaymentMethodId = dto.PaymentMethodId,
                Notes = dto.Notes,
                CustomerId = dto.CustomerId,
                UserId = finalUserId
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
                // Validar CustomerId solo si NO es venta a socia
                if (dto.CustomerId == null && dto.PartnerUserId == null)
                    throw new BusinessException(
                        "Las ventas a crédito requieren un cliente registrado.");

                // Para ventas a socias, usar 1 cuota por defecto si no se especifica
                var numberOfInstallments = dto.NumberOfInstallments ?? 1;
                if (numberOfInstallments < 1)
                    numberOfInstallments = 1;

                var credit = new Credit
                {
                    SaleId = sale.Id,
                    CustomerId = dto.CustomerId, // Puede ser null para ventas a socias
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
            // BUG 1 FIX: Fórmula correcta para cálculo de partnerPrice y earnings
            var seller = await _unitOfWork.Users.GetByIdAsync(finalUserId);
            if (seller?.Role == UserRole.Partner)
            {
                var partnerConfig = (await _unitOfWork.PartnerConfigs
                    .FindAsync(pc => pc.UserId == finalUserId && pc.IsActive))
                    .FirstOrDefault();
                if (partnerConfig != null)
                {
                    foreach (var detailDto in dto.Details)
                    {
                        var product = await _unitOfWork.Products
                            .GetByIdAsync(detailDto.ProductId);
                        
                        // Determinar porcentaje según tipo de producto
                        var commissionPercent = product!.IsPartnership
                            ? partnerConfig.AllianceCommissionPercent
                            : partnerConfig.CommissionPercent;

                        // BUG 1 FIX: Fórmula correcta
                        var gainAS = product.SalePrice - product.Cost;
                        var partnerPrice = product.SalePrice - (gainAS * commissionPercent / 100);
                        var partnerEarning = gainAS * commissionPercent / 100;
                        var asEarning = gainAS - partnerEarning;
                        
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