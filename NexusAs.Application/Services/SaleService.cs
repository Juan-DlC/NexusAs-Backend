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
        private readonly ISaleRepository _saleRepository;
        private readonly IProductRepository _productRepository;

        public SaleService(IUnitOfWork unitOfWork, IMapper mapper, ISaleRepository saleRepository, IProductRepository productRepository)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _saleRepository = saleRepository;
            _productRepository = productRepository;
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
            // Verificar idempotencia para prevenir ventas duplicadas
            if (!string.IsNullOrEmpty(dto.RequestId))
            {
                var existingSales = await _unitOfWork.Sales.FindAsync(s => 
                    s.RequestId == dto.RequestId && 
                    s.Date >= DateTime.Now.AddMinutes(-5));
                var existing = existingSales.FirstOrDefault();
                
                if (existing != null)
                    return _mapper.Map<SaleDto>(existing);
            }
            
            // Filtrar detalles vacíos (ProductId <= 0) antes de procesar
            var validDetails = dto.Details.Where(d => d.ProductId > 0).ToList();
            
            if (!validDetails.Any())
                throw new BusinessException("La venta debe tener al menos un producto válido.");
            
            // TAREA 1: OPTIMIZACIÓN - Cargar TODOS los productos necesarios en UNA sola consulta
            var productIds = validDetails.Select(d => d.ProductId).Distinct().ToList();
            var products = await _productRepository.GetProductsByIdsAsync(productIds);

            // Validar stock de todos los productos antes de crear la venta
            foreach (var detail in validDetails)
            {
                if (!products.TryGetValue(detail.ProductId, out var product))
                    throw new NotFoundException(nameof(Product), detail.ProductId);
                
                if (product.Stock < detail.Quantity)
                    throw new BusinessException(
                        $"Stock insuficiente para '{product.Name}'. " + 
                        $"Disponible: {product.Stock}, Solicitado: {detail.Quantity}.");
            }

            // Determinar el UserId final (socia o usuario actual)
            int finalUserId = dto.PartnerUserId ?? userId;

            // Validar que el userId existe en la tabla Users
            var userExists = await _unitOfWork.Users.GetByIdAsync(finalUserId);
            if (userExists == null)
            {
                throw new BusinessException($"El usuario con Id {finalUserId} no existe en el sistema. Verifique que la socia esté correctamente registrada.");
            }

            // Validación explícita de PaymentMethodId ANTES de crear la venta
            if (dto.PaymentMethodId <= 0)
                throw new BusinessException("Debe seleccionar un método de pago válido.");

            var paymentMethod = await _unitOfWork.PaymentMethods.GetByIdAsync(dto.PaymentMethodId);
            if (paymentMethod == null)
                throw new BusinessException($"El método de pago seleccionado no existe.");

            // VALIDACIÓN CRÍTICA: Si es crédito, DEBE tener cliente (excepto si es venta a socia)
            if (paymentMethod.Code == "CREDIT")
            {
                if (dto.CustomerId == null && dto.PartnerUserId == null)
                    throw new BusinessException(
                        "Las ventas a crédito requieren seleccionar un cliente registrado.");
            }

            // TAREA 1: OPTIMIZACIÓN - Cargar PartnerConfig UNA sola vez si es necesario
            PartnerConfig? partnerConfig = null;
            if (dto.PartnerUserId.HasValue || userExists.Role == UserRole.Partner)
            {
                var configs = await _unitOfWork.PartnerConfigs
                    .FindAsync(pc => pc.UserId == finalUserId && pc.IsActive);
                partnerConfig = configs.FirstOrDefault();
            }

            // Calcular automáticamente precio de socia si UnitPrice es 0
            if (partnerConfig != null)
            {
                foreach (var detailDto in validDetails)
                {
                    if (detailDto.UnitPrice <= 0)
                    {
                        if (products.TryGetValue(detailDto.ProductId, out var product))
                        {
                            var gainAS = product.SalePrice - product.Cost;
                            var commissionPercent = product.IsPartnership
                                ? partnerConfig.AllianceCommissionPercent
                                : partnerConfig.CommissionPercent;
                            
                            // Precio para socia = PrecioVenta - (Ganancia × Comisión / 100)
                            detailDto.UnitPrice = product.SalePrice - (gainAS * commissionPercent / 100);
                        }
                    }
                }
            }

            // TAREA 3: OPTIMIZACIÓN - Generación eficiente de número de factura
            var lastSale = await _saleRepository.GetLastSaleNumberAsync();
            
            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastSale))
            {
                var parts = lastSale.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[1], out int last))
                    nextNumber = last + 1;
            }
            var saleNumber = $"FAC-{nextNumber:D4}";
            
            // Calcular totales (usando solo detalles válidos)
            var subtotal = validDetails.Sum(d => d.Quantity * d.UnitPrice);
            
            // Calcular descuento por porcentaje o monto fijo
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
#pragma warning disable CS0618 // Compatibilidad con versión anterior
            else if (dto.Discount > 0)
            {
                discount = dto.Discount;
            }
#pragma warning restore CS0618
            
            var total = subtotal - discount;

            // Crear la venta
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
                UserId = finalUserId,
                ProcessedByUserId = userId,  // Admin que procesó la venta
                RequestId = dto.RequestId     // Para prevenir duplicados
            };
            await _unitOfWork.Sales.AddAsync(sale);
            await _unitOfWork.SaveChangesAsync();

            // Crear detalles, descontar stock y registrar movimientos (solo detalles válidos)
            foreach (var detailDto in validDetails)
            {
                if (!products.TryGetValue(detailDto.ProductId, out var product))
                    continue; // Ya validado anteriormente

                // Crear detalle de venta
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
                var stockBefore = product.Stock;
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

            // Si es crédito, crear registro de crédito
            if (paymentMethod.Code == "CREDIT")
            {
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

            // Si el vendedor es Partner, registrar ganancias (solo detalles válidos)
            if (userExists.Role == UserRole.Partner && partnerConfig != null)
            {
                foreach (var detailDto in validDetails)
                {
                    if (!products.TryGetValue(detailDto.ProductId, out var product))
                        continue;
                    
                    // Determinar porcentaje según tipo de producto
                    var commissionPercent = product.IsPartnership
                        ? partnerConfig.AllianceCommissionPercent
                        : partnerConfig.CommissionPercent;

                    // Fórmula correcta
                    var gainAS = product.SalePrice - product.Cost;
                    var partnerEarning = gainAS * commissionPercent / 100;
                    var asEarning = gainAS - partnerEarning;
                    var partnerPrice = product.SalePrice - partnerEarning;
                    
                    var partnerSale = new PartnerSale
                    {
                        PartnerConfigId = partnerConfig.Id,
                        SaleId = sale.Id,
                        ProductId = detailDto.ProductId,
                        Quantity = detailDto.Quantity,
                        CostPrice = product.Cost,
                        PartnerPrice = Math.Round(partnerPrice, 0),
                        SalePrice = product.SalePrice,
                        CommissionPercent = commissionPercent,
                        PartnerEarning = Math.Round(partnerEarning * detailDto.Quantity, 0),
                        AsEarning = Math.Round(asEarning * detailDto.Quantity, 0),
                        IsPartnership = product.IsPartnership,
                        Date = DateTime.Now
                    };
                    await _unitOfWork.PartnerSales.AddAsync(partnerSale);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<SaleDto>(sale);
        }
    }
}