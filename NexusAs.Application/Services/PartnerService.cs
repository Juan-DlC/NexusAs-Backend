using AutoMapper;
using NexusAs.Application.DTOs.Partners;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Entities;
using NexusAs.Domain.Enums;
using NexusAs.Domain.Exceptions;
using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Sales;

namespace NexusAs.Application.Services
{
    public class PartnerService : IPartnerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPartnerReportService _partnerReportService;
        private readonly ISaleService _saleService;
        private readonly IProductRepository _productRepository;

        public PartnerService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IPartnerReportService partnerReportService,
            ISaleService saleService,
            IProductRepository productRepository)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _partnerReportService = partnerReportService;
            _saleService = saleService;
            _productRepository = productRepository;
        }

        public async Task<AllianceReportDto> GetAllianceReportAsync(DateTime from, DateTime to)
        {
            return await _partnerReportService.GetAllianceReportAsync(from, to);
        }

        public async Task<IEnumerable<PartnerConfigDto>> GetAllPartnersAsync()
        {
            var configs = await _unitOfWork.PartnerConfigs.GetAllAsync();
            var result = new List<PartnerConfigDto>();

            foreach (var config in configs)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(config.UserId);
                
                // Cargar comisiones específicas de BusinessPartners
                var businessCommissions = await _unitOfWork.PartnerBusinessCommissions
                    .FindAsync(pbc => pbc.PartnerConfigId == config.Id && pbc.IsActive);
                
                var businessCommissionDtos = new List<PartnerBusinessCommissionDto>();
                foreach (var bc in businessCommissions)
                {
                    var businessPartner = await _unitOfWork.BusinessPartners.GetByIdAsync(bc.BusinessPartnerId);
                    businessCommissionDtos.Add(new PartnerBusinessCommissionDto
                    {
                        BusinessPartnerId = bc.BusinessPartnerId,
                        BusinessPartnerName = businessPartner?.Name ?? "",
                        CommissionPercent = bc.CommissionPercent
                    });
                }

                result.Add(new PartnerConfigDto
                {
                    Id = config.Id,
                    UserId = config.UserId,
                    PartnerName = user?.FullName ?? "",
                    Username = user?.Username ?? "",
                    CommissionPercent = config.CommissionPercent,
                    AllianceCommissionPercent = config.AllianceCommissionPercent,
                    IsActive = config.IsActive,
                    Notes = config.Notes,
                    BusinessCommissions = businessCommissionDtos
                });
            }

            return result;
        }

        public async Task<PartnerConfigDto?> GetPartnerByIdAsync(int partnerConfigId)
        {
            var config = await _unitOfWork.PartnerConfigs.GetByIdAsync(partnerConfigId);
            if (config == null)
                throw new NotFoundException("PartnerConfig", partnerConfigId);

            var user = await _unitOfWork.Users.GetByIdAsync(config.UserId);
            
            // Cargar comisiones específicas de BusinessPartners
            var businessCommissions = await _unitOfWork.PartnerBusinessCommissions
                .FindAsync(pbc => pbc.PartnerConfigId == config.Id && pbc.IsActive);
            
            var businessCommissionDtos = new List<PartnerBusinessCommissionDto>();
            foreach (var bc in businessCommissions)
            {
                var businessPartner = await _unitOfWork.BusinessPartners.GetByIdAsync(bc.BusinessPartnerId);
                businessCommissionDtos.Add(new PartnerBusinessCommissionDto
                {
                    BusinessPartnerId = bc.BusinessPartnerId,
                    BusinessPartnerName = businessPartner?.Name ?? "",
                    CommissionPercent = bc.CommissionPercent
                });
            }

            return new PartnerConfigDto
            {
                Id = config.Id,
                UserId = config.UserId,
                PartnerName = user?.FullName ?? "",
                Username = user?.Username ?? "",
                CommissionPercent = config.CommissionPercent,
                AllianceCommissionPercent = config.AllianceCommissionPercent,
                IsActive = config.IsActive,
                Notes = config.Notes,
                BusinessCommissions = businessCommissionDtos
            };
        }

        public async Task<PartnerConfigDto> CreatePartnerConfigAsync(CreatePartnerConfigDto dto)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(dto.UserId);
            if (user == null)
                throw new NotFoundException("User", dto.UserId);

            if (user.Role != UserRole.Partner)
                throw new BusinessException(
                    "Solo usuarios con rol Partner pueden tener configuración de socia.");

            var exists = await _unitOfWork.PartnerConfigs
                .ExistsAsync(p => p.UserId == dto.UserId && p.IsActive);
            if (exists)
                throw new BusinessException(
                    "Esta usuaria ya tiene una configuración de socia activa.");

            var config = new PartnerConfig
            {
                UserId = dto.UserId,
                CommissionPercent = dto.CommissionPercent,
                Notes = dto.Notes
            };

            await _unitOfWork.PartnerConfigs.AddAsync(config);
            await _unitOfWork.SaveChangesAsync();

            return new PartnerConfigDto
            {
                Id = config.Id,
                UserId = config.UserId,
                PartnerName = user.FullName,
                Username = user.Username,
                CommissionPercent = config.CommissionPercent,
                IsActive = config.IsActive,
                Notes = config.Notes
            };
        }

        public async Task<PartnerConfigDto> UpdateCommissionAsync(
            int partnerConfigId, UpdatePartnerCommissionDto dto)
        {
            var config = await _unitOfWork.PartnerConfigs.GetByIdAsync(partnerConfigId);
            if (config == null)
                throw new NotFoundException("PartnerConfig", partnerConfigId);

            if (dto.CommissionPercent <= 0 || dto.CommissionPercent > 100)
                throw new BusinessException(
                    "El porcentaje de comisión debe estar entre 1 y 100.");

            if (dto.AllianceCommissionPercent < 0 || dto.AllianceCommissionPercent > 100)
                throw new BusinessException(
                    "El porcentaje de comisión de alianza debe estar entre 0 y 100.");

            // Actualizar comisiones generales (fallback)
            config.CommissionPercent = dto.CommissionPercent;
            config.AllianceCommissionPercent = dto.AllianceCommissionPercent;
            _unitOfWork.PartnerConfigs.Update(config);

            // Procesar comisiones individuales por BusinessPartner
            foreach (var bc in dto.BusinessCommissions)
            {
                if (bc.CommissionPercent < 0 || bc.CommissionPercent > 100)
                    throw new BusinessException(
                        $"El porcentaje de comisión para el socio comercial {bc.BusinessPartnerId} debe estar entre 0 y 100.");

                var existingCommissions = await _unitOfWork.PartnerBusinessCommissions
                    .FindAsync(c => c.PartnerConfigId == partnerConfigId && 
                                   c.BusinessPartnerId == bc.BusinessPartnerId);
                var existing = existingCommissions.FirstOrDefault();

                if (existing != null)
                {
                    // Actualizar comisión existente
                    existing.CommissionPercent = bc.CommissionPercent;
                    _unitOfWork.PartnerBusinessCommissions.Update(existing);
                }
                else
                {
                    // Crear nueva comisión específica
                    var newCommission = new PartnerBusinessCommission
                    {
                        PartnerConfigId = partnerConfigId,
                        BusinessPartnerId = bc.BusinessPartnerId,
                        CommissionPercent = bc.CommissionPercent
                    };
                    await _unitOfWork.PartnerBusinessCommissions.AddAsync(newCommission);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            return await GetPartnerByIdAsync(partnerConfigId) ?? throw new Exception();
        }

        public async Task<PartnerProductPriceDto> SetProductPriceAsync(
            int partnerConfigId, SetPartnerProductPriceDto dto)
        {
            var config = await _unitOfWork.PartnerConfigs.GetByIdAsync(partnerConfigId);
            if (config == null)
                throw new NotFoundException("PartnerConfig", partnerConfigId);

            var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
            if (product == null)
                throw new NotFoundException("Product", dto.ProductId);

            if (!product.IsPartnership)
                throw new BusinessException(
                    "Solo productos de alianza pueden tener precio especial por socia.");

            var existing = (await _unitOfWork.PartnerProductPrices
                .FindAsync(p => p.PartnerConfigId == partnerConfigId
                    && p.ProductId == dto.ProductId
                    && p.IsActive))
                .FirstOrDefault();

            if (existing != null)
            {
                existing.CustomCommission = dto.CustomCommission;
                _unitOfWork.PartnerProductPrices.Update(existing);
            }
            else
            {
                existing = new PartnerProductPrice
                {
                    PartnerConfigId = partnerConfigId,
                    ProductId = dto.ProductId,
                    CustomCommission = dto.CustomCommission
                };
                await _unitOfWork.PartnerProductPrices.AddAsync(existing);
            }

            await _unitOfWork.SaveChangesAsync();

            return new PartnerProductPriceDto
            {
                Id = existing.Id,
                ProductId = dto.ProductId,
                ProductName = product.Name,
                CustomCommission = dto.CustomCommission
            };
        }

        public async Task<IEnumerable<PartnerProductPriceDto>> GetProductPricesAsync(
            int partnerConfigId)
        {
            var prices = await _unitOfWork.PartnerProductPrices
                .FindAsync(p => p.PartnerConfigId == partnerConfigId && p.IsActive);

            var result = new List<PartnerProductPriceDto>();
            foreach (var price in prices)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(price.ProductId);
                result.Add(new PartnerProductPriceDto
                {
                    Id = price.Id,
                    ProductId = price.ProductId,
                    ProductName = product?.Name ?? "",
                    CustomCommission = price.CustomCommission
                });
            }
            return result;
        }

        public async Task<PartnerSummaryDto> GetPartnerSummaryAsync(int partnerConfigId)
        {
            var config = await _unitOfWork.PartnerConfigs.GetByIdAsync(partnerConfigId);
            if (config == null)
                throw new NotFoundException("PartnerConfig", partnerConfigId);

            var user = await _unitOfWork.Users.GetByIdAsync(config.UserId);
            
            // Calcular deuda correctamente (solo créditos pendientes)
            var (totalDebt, totalPaid) = await CalculatePartnerDebtAsync(config.UserId);
            
            var sales = await _unitOfWork.PartnerSales
                .FindAsync(ps => ps.PartnerConfigId == partnerConfigId);

            return new PartnerSummaryDto
            {
                PartnerId = partnerConfigId,
                PartnerName = user?.FullName ?? "",
                TotalSales = sales.Sum(s => s.SalePrice * s.Quantity),
                TotalEarnings = sales.Sum(s => s.PartnerEarning),
                TotalDebt = totalDebt,
                TotalPaid = totalPaid,
                PendingDebt = totalDebt - totalPaid
            };
        }

        public async Task<IEnumerable<PartnerSaleDto>> GetPartnerSalesAsync(
    int partnerConfigId,
    DateTime? from = null,
    DateTime? to = null)
        {
            var sales = await _unitOfWork.PartnerSales
                .FindAsync(ps =>
                    ps.PartnerConfigId == partnerConfigId &&
                    (from == null || ps.Date >= from) &&
                    (to == null || ps.Date <= to));
            var result = new List<PartnerSaleDto>();
            foreach (var sale in sales)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(sale.ProductId);
                var saleRecord = await _unitOfWork.Sales.GetByIdAsync(sale.SaleId);
                result.Add(new PartnerSaleDto
                {
                    Id = sale.Id,
                    ProductName = product?.Name ?? "",
                    Quantity = sale.Quantity,
                    CostPrice = sale.CostPrice,
                    PartnerPrice = sale.PartnerPrice,
                    SalePrice = sale.SalePrice,
                    CommissionPercent = sale.CommissionPercent,
                    PartnerEarning = sale.PartnerEarning,
                    AsEarning = sale.AsEarning,
                    IsPartnership = sale.IsPartnership,
                    Date = sale.Date,
                    SaleNumber = saleRecord?.SaleNumber ?? ""
                });
            }
            return result;
        }

        public async Task<PartnerLiquidationDto> RegisterLiquidationAsync(
            int partnerConfigId,
            RegisterPartnerLiquidationDto dto)
        {
            var config = await _unitOfWork.PartnerConfigs.GetByIdAsync(partnerConfigId);
            if (config == null)
                throw new NotFoundException("PartnerConfig", partnerConfigId);

            if (dto.Amount <= 0)
                throw new BusinessException("El monto debe ser mayor a 0.");

            if (!Enum.TryParse<LiquidationType>(dto.Type, out var type))
                throw new BusinessException("Tipo inválido. Use: Payment o Earning.");

            // VALIDACIÓN MEJORADA: Validar que el abono no supere la deuda
            if (type == LiquidationType.Payment)
            {
                // Si el abono es a una factura específica, validar contra esa factura
                if (dto.SaleId.HasValue)
                {
                    var sale = await _unitOfWork.Sales.GetByIdAsync(dto.SaleId.Value);
                    if (sale == null)
                        throw new NotFoundException("Sale", dto.SaleId.Value);

                    // Buscar el crédito asociado a esta venta
                    var credits = await _unitOfWork.Credits.FindAsync(c => 
                        c.SaleId == dto.SaleId.Value && c.IsActive);
                    var credit = credits.FirstOrDefault();

                    if (credit == null)
                        throw new BusinessException("Esta factura no tiene un crédito asociado.");

                    if (credit.PendingAmount <= 0)
                        throw new BusinessException("Esta factura ya está completamente pagada.");

                    if (dto.Amount > credit.PendingAmount)
                        throw new BusinessException(
                            $"El abono (${dto.Amount:N0}) supera el saldo pendiente de esta factura (${credit.PendingAmount:N0}).");
                }
                else
                {
                    // Si es abono general (sin SaleId), validar contra la deuda total
                    var (totalDebt, totalPaid) = await CalculatePartnerDebtAsync(config.UserId);
                    var pendingDebt = totalDebt - totalPaid;

                    if (pendingDebt <= 0)
                        throw new BusinessException("Esta socia no tiene deuda pendiente.");

                    if (dto.Amount > pendingDebt)
                        throw new BusinessException(
                            $"El abono (${dto.Amount:N0}) supera la deuda pendiente total (${pendingDebt:N0}).");
                }
            }

            var liquidation = new PartnerLiquidation
            {
                PartnerConfigId = partnerConfigId,
                Amount = dto.Amount,
                Type = type,
                Date = DateTime.Now,
                Notes = dto.Notes,
                PeriodFrom = dto.PeriodFrom,
                PeriodTo = dto.PeriodTo,
                SaleId = dto.SaleId
            };

            await _unitOfWork.PartnerLiquidations.AddAsync(liquidation);
            await _unitOfWork.SaveChangesAsync();

            // Si es un abono (Payment), actualizar los créditos correspondientes
            if (type == LiquidationType.Payment)
            {
                if (dto.SaleId.HasValue)
                {
                    // Abono a una factura específica
                    var credits = await _unitOfWork.Credits.FindAsync(c => 
                        c.SaleId == dto.SaleId.Value && c.IsActive);
                    var credit = credits.FirstOrDefault();
                    
                    if (credit != null)
                    {
                        await ApplyCreditPaymentAsync(credit, dto.Amount);
                    }
                }
                else
                {
                    // Abono general: distribuir entre todos los créditos pendientes de la socia
                    var salesWithCredit = await _unitOfWork.Sales.FindAsync(s => 
                        s.UserId == config.UserId && s.IsActive);
                    
                    var pendingCredits = new List<Credit>();
                    foreach (var sale in salesWithCredit)
                    {
                        var credits = await _unitOfWork.Credits.FindAsync(c => 
                            c.SaleId == sale.Id && c.IsActive && c.PendingAmount > 0);
                        pendingCredits.AddRange(credits);
                    }
                    
                    // Ordenar por fecha (más antiguas primero - FIFO)
                    var sortedCredits = pendingCredits
                        .OrderBy(c => c.CreatedAt)
                        .ToList();
                    
                    // Distribuir el abono general entre los créditos pendientes
                    var remainingAmount = dto.Amount;
                    foreach (var credit in sortedCredits)
                    {
                        if (remainingAmount <= 0) break;
                        
                        var amountToApply = Math.Min(remainingAmount, credit.PendingAmount);
                        await ApplyCreditPaymentAsync(credit, amountToApply);
                        remainingAmount -= amountToApply;
                    }
                }
            }

            // Obtener el SaleNumber para el DTO de respuesta
            string? saleNumber = null;
            if (dto.SaleId.HasValue)
            {
                var sale = await _unitOfWork.Sales.GetByIdAsync(dto.SaleId.Value);
                saleNumber = sale?.SaleNumber;
            }

            return new PartnerLiquidationDto
            {
                Id = liquidation.Id,
                Amount = liquidation.Amount,
                Type = liquidation.Type.ToString(),
                Date = liquidation.Date,
                Notes = liquidation.Notes,
                PeriodFrom = liquidation.PeriodFrom,
                PeriodTo = liquidation.PeriodTo,
                SaleNumber = saleNumber
            };
        }

        public async Task<decimal> CalculatePartnerPriceAsync(int partnerConfigId, int productId)
        {
            var partnerConfig = await _unitOfWork.PartnerConfigs.GetByIdAsync(partnerConfigId);
            if (partnerConfig == null)
                throw new NotFoundException("PartnerConfig", partnerConfigId);

            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
                throw new NotFoundException("Product", productId);

            // TAREA 2: Determinar porcentaje según tipo de producto
            var commissionPercent = product.IsPartnership
                ? partnerConfig.AllianceCommissionPercent
                : partnerConfig.CommissionPercent;

            // TAREA 2: Fórmula correcta partnerPrice = SalePrice - (gainAS × commissionPercent / 100)
            var gainAS = product.SalePrice - product.Cost;
            return Math.Round(product.SalePrice - (gainAS * commissionPercent / 100), 0);
        }

        // TAREA 1 FIX: Usar repositorio específico con Include de Category y Supplier
        public async Task<IEnumerable<PartnerProductViewDto>> GetMyProductsAsync(int partnerConfigId)
        {
            var partnerConfig = await _unitOfWork.PartnerConfigs.GetByIdAsync(partnerConfigId);
            if (partnerConfig == null)
                throw new NotFoundException("PartnerConfig", partnerConfigId);

            // Cargar comisiones específicas de BusinessPartners para esta socia
            var businessCommissions = await _unitOfWork.PartnerBusinessCommissions
                .FindAsync(pbc => pbc.PartnerConfigId == partnerConfigId && pbc.IsActive);
            var businessCommissionsDict = businessCommissions
                .ToDictionary(bc => bc.BusinessPartnerId, bc => bc.CommissionPercent);

            // Usar repositorio con Include de Category y Supplier
            var products = await _productRepository.GetProductsWithCategoryAsync();

            var result = new List<PartnerProductViewDto>();
            foreach (var product in products.Where(p => p.IsActive && p.Stock > 0).OrderBy(p => p.Name))
            {
                // Determinar porcentaje según tipo de producto
                decimal commissionPercent;
                if (product.IsPartnership && product.BusinessPartnerId.HasValue)
                {
                    // Buscar comisión específica para este BusinessPartner
                    if (businessCommissionsDict.TryGetValue(product.BusinessPartnerId.Value, out var specificPercent))
                    {
                        commissionPercent = specificPercent;
                    }
                    else
                    {
                        // Fallback al porcentaje general de alianza
                        commissionPercent = partnerConfig.AllianceCommissionPercent;
                    }
                }
                else
                {
                    commissionPercent = partnerConfig.CommissionPercent;
                }

                // Fórmula correcta partnerPrice = SalePrice - (gainAS × commissionPercent / 100)
                var gainAS = product.SalePrice - product.Cost;
                var partnerPrice = product.SalePrice - (gainAS * commissionPercent / 100);

                result.Add(new PartnerProductViewDto
                {
                    ProductId = product.Id,
                    Code = product.Code,
                    Name = product.Name,
                    CategoryName = product.Category?.Name ?? string.Empty,
                    Stock = product.Stock,
                    PartnerPrice = Math.Round(partnerPrice, 0),
                    SuggestedPrice = product.SalePrice,
                    IsPartnership = product.IsPartnership
                });
            }

            return result;
        }

        public async Task<IEnumerable<PartnerLiquidationDto>> GetLiquidationsAsync(
            int partnerConfigId)
        {
            var liquidations = await _unitOfWork.PartnerLiquidations
                .FindAsync(pl => pl.PartnerConfigId == partnerConfigId);

            var result = new List<PartnerLiquidationDto>();
            foreach (var l in liquidations)
            {
                string? saleNumber = null;
                
                // Si la liquidación está vinculada a una venta, obtener el número de factura
                if (l.SaleId.HasValue)
                {
                    var sale = await _unitOfWork.Sales.GetByIdAsync(l.SaleId.Value);
                    saleNumber = sale?.SaleNumber;
                }

                result.Add(new PartnerLiquidationDto
                {
                    Id = l.Id,
                    Amount = l.Amount,
                    Type = l.Type.ToString(),
                    Date = l.Date,
                    Notes = l.Notes,
                    PeriodFrom = l.PeriodFrom,
                    PeriodTo = l.PeriodTo,
                    SaleNumber = saleNumber
                });
            }

            return result.OrderByDescending(x => x.Date);
        }

        // TAREA 3: Paginación en liquidaciones de socias
        public async Task<PagedResponseDto<PartnerLiquidationDto>> GetLiquidationsPagedAsync(
            int partnerConfigId, int pageNumber, int pageSize)
        {
            var config = await _unitOfWork.PartnerConfigs.GetByIdAsync(partnerConfigId);
            if (config == null)
                throw new NotFoundException("PartnerConfig", partnerConfigId);

            var liquidations = await _unitOfWork.PartnerLiquidations
                .FindAsync(pl => pl.PartnerConfigId == partnerConfigId && pl.IsActive);

            var totalRecords = liquidations.Count();

            var pagedLiquidations = liquidations
                .OrderByDescending(pl => pl.Date)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new List<PartnerLiquidationDto>();
            foreach (var l in pagedLiquidations)
            {
                string? saleNumber = null;
                
                if (l.SaleId.HasValue)
                {
                    var sale = await _unitOfWork.Sales.GetByIdAsync(l.SaleId.Value);
                    saleNumber = sale?.SaleNumber;
                }

                result.Add(new PartnerLiquidationDto
                {
                    Id = l.Id,
                    Amount = l.Amount,
                    Type = l.Type.ToString(),
                    Date = l.Date,
                    Notes = l.Notes,
                    PeriodFrom = l.PeriodFrom,
                    PeriodTo = l.PeriodTo,
                    SaleNumber = saleNumber
                });
            }

            return new PagedResponseDto<PartnerLiquidationDto>
            {
                Data = result,
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<byte[]> GeneratePartnerStatementAsync(
            int partnerConfigId,
            DateTime from,
            DateTime to)
        {
            return await _partnerReportService
                .GeneratePartnerStatementPdfAsync(partnerConfigId, from, to);
        }

        // TAREA 2: OPTIMIZACIÓN - Mantener lógica simplificada sin múltiples consultas innecesarias
        public async Task<PartnerAdminSummaryDto> GetAdminSummaryAsync(int partnerConfigId)
        {
            var config = await _unitOfWork.PartnerConfigs.GetByIdAsync(partnerConfigId);
            if (config == null)
                throw new NotFoundException("PartnerConfig", partnerConfigId);

            var user = await _unitOfWork.Users.GetByIdAsync(config.UserId);
            
            // Calcular deuda correctamente (solo créditos pendientes)
            var (totalDebt, totalPaid) = await CalculatePartnerDebtAsync(config.UserId);
            
            var sales = await _unitOfWork.PartnerSales
                .FindAsync(ps => ps.PartnerConfigId == partnerConfigId && ps.IsActive);

            var invoiceCount = sales.Select(s => s.SaleId).Distinct().Count();

            return new PartnerAdminSummaryDto
            {
                PartnerId = partnerConfigId,
                PartnerName = user?.FullName ?? "",
                TotalDebt = totalDebt,
                TotalPaid = totalPaid,
                PendingDebt = Math.Max(0, totalDebt - totalPaid),
                InvoiceCount = invoiceCount,
                CommissionPercent = config.CommissionPercent,
                AllianceCommissionPercent = config.AllianceCommissionPercent
            };
        }

        // Método auxiliar para calcular deuda correcta
        // Solo cuenta ventas a crédito y usa el PendingAmount del Credit
        private async Task<(decimal totalDebt, decimal totalPaid)> CalculatePartnerDebtAsync(int partnerUserId)
        {
            // Obtener todas las ventas de la socia que tienen crédito
            var salesWithCredit = await _unitOfWork.Sales.FindAsync(s => 
                s.UserId == partnerUserId && s.IsActive);
            
            decimal totalDebt = 0;
            decimal totalPaid = 0;

            foreach (var sale in salesWithCredit)
            {
                // Buscar si esta venta tiene crédito
                var credits = await _unitOfWork.Credits.FindAsync(c => 
                    c.SaleId == sale.Id && c.IsActive);
                var credit = credits.FirstOrDefault();

                if (credit != null)
                {
                    // Venta a crédito: suma el total y lo abonado
                    totalDebt += credit.TotalAmount;
                    totalPaid += credit.PaidAmount;
                }
                // Si no tiene crédito (venta de contado), no suma nada a la deuda
            }

            return (totalDebt, totalPaid);
        }

        // Método auxiliar para aplicar un pago a un crédito específico
        private async Task ApplyCreditPaymentAsync(Credit credit, decimal amount)
        {
            credit.PaidAmount += amount;
            credit.PendingAmount = credit.TotalAmount - credit.PaidAmount;
            
            // Actualizar estado del crédito
            if (credit.PendingAmount <= 0)
                credit.Status = CreditStatus.Paid;
            else if (credit.PaidAmount > 0 && credit.PaidAmount < credit.TotalAmount)
                credit.Status = CreditStatus.Partial;
            else
                credit.Status = CreditStatus.Pending;
            
            _unitOfWork.Credits.Update(credit);
            
            // Distribuir el abono en las cuotas pendientes
            var pendingInstallments = await _unitOfWork.CreditInstallments.FindAsync(i => 
                i.CreditId == credit.Id && !i.IsPaid);
            var installmentsList = pendingInstallments.OrderBy(i => i.Number).ToList();
            
            var remaining = amount;
            foreach (var inst in installmentsList)
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
                _unitOfWork.CreditInstallments.Update(inst);
            }
            
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<PagedResponseDto<PartnerInvoiceDto>> GetPartnerInvoicesAsync(
            int partnerConfigId, int pageNumber, int pageSize)
        {
            var config = await _unitOfWork.PartnerConfigs.GetByIdAsync(partnerConfigId);
            if (config == null)
                throw new NotFoundException("PartnerConfig", partnerConfigId);

            // Obtener ventas con PaymentMethodEntity y Credit incluidos
            var (sales, totalRecords) = await _unitOfWork.Sales.GetSalesWithDetailsAsync(
                config.UserId, "Partner", null, null, null, pageNumber, pageSize);

            var invoices = new List<PartnerInvoiceDto>();
            foreach (var sale in sales)
            {
                var invoice = new PartnerInvoiceDto
                {
                    SaleId = sale.Id,
                    SaleNumber = sale.SaleNumber,
                    Date = sale.Date,
                    Total = sale.Total,
                    PaymentMethodName = sale.PaymentMethodEntity?.Name ?? "Desconocido",
                    CreditStatus = DetermineCreditStatus(sale),
                    PendingAmount = sale.Credit?.PendingAmount ?? 0,
                    PaidAmount = sale.Credit?.PaidAmount ?? 0,
                    IsFullyReturned = sale.Status == SaleStatus.FullReturn || sale.Total == 0
                };

                invoices.Add(invoice);
            }

            return new PagedResponseDto<PartnerInvoiceDto>
            {
                Data = invoices,
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Determina el estado del crédito basándose en el método de pago y el objeto Credit
        /// </summary>
        private string DetermineCreditStatus(Sale sale)
        {
            // PASO 1: Verificar si el método de pago es crédito
            // Si NO es crédito, retornar "NoCredit" inmediatamente
            if (sale.PaymentMethodEntity == null || sale.PaymentMethodEntity.Code != "CREDIT")
                return "NoCredit";

            // PASO 2: El método de pago ES crédito, verificar el objeto Credit
            // Si no existe el objeto Credit, el crédito está pendiente (recién creado)
            if (sale.Credit == null)
                return "Pending";

            // PASO 3: El objeto Credit existe, usar su estado
            return sale.Credit.Status switch
            {
                CreditStatus.Pending => "Pending",
                CreditStatus.Partial => "Partial",
                CreditStatus.Paid => "Paid",
                _ => "Pending" // Fallback por seguridad
            };
        }

        public async Task<SaleDto> GetPartnerInvoiceDetailAsync(int partnerUserId, int saleId)
        {
            var sale = await _unitOfWork.Sales.GetSaleByIdWithDetailsAsync(saleId);
            if (sale == null)
                throw new NotFoundException("Sale", saleId);

            // Validar que la venta pertenece a la socia
            if (sale.UserId != partnerUserId)
                throw new BusinessException("Esta factura no pertenece a la socia especificada.");

            return _mapper.Map<SaleDto>(sale);
        }
    }
}
