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

        public PartnerService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IPartnerReportService partnerReportService,
            ISaleService saleService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _partnerReportService = partnerReportService;
            _saleService = saleService;
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
                result.Add(new PartnerConfigDto
                {
                    Id = config.Id,
                    UserId = config.UserId,
                    PartnerName = user?.FullName ?? "",
                    Username = user?.Username ?? "",
                    CommissionPercent = config.CommissionPercent,
                    IsActive = config.IsActive,
                    Notes = config.Notes
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
            return new PartnerConfigDto
            {
                Id = config.Id,
                UserId = config.UserId,
                PartnerName = user?.FullName ?? "",
                Username = user?.Username ?? "",
                CommissionPercent = config.CommissionPercent,
                AllianceCommissionPercent = config.AllianceCommissionPercent,
                IsActive = config.IsActive,
                Notes = config.Notes
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

            config.CommissionPercent = dto.CommissionPercent;
            config.AllianceCommissionPercent = dto.AllianceCommissionPercent;
            _unitOfWork.PartnerConfigs.Update(config);
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
            
            // BUG 1 FIX: Calcular deuda correctamente (solo créditos pendientes)
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

            // VALIDAR QUE EL ABONO NO SUPERE LA DEUDA (solo para Payment)
            if (type == LiquidationType.Payment)
            {
                var sales = await _unitOfWork.PartnerSales
                    .FindAsync(ps => ps.PartnerConfigId == partnerConfigId && ps.IsActive);
                var liquidations = await _unitOfWork.PartnerLiquidations
                    .FindAsync(pl => pl.PartnerConfigId == partnerConfigId && pl.IsActive);

                var totalDebt = sales.Sum(s => s.PartnerPrice * s.Quantity);
                var totalPaid = liquidations
                    .Where(l => l.Type == LiquidationType.Payment)
                    .Sum(l => l.Amount);
                var pendingDebt = totalDebt - totalPaid;

                if (pendingDebt <= 0)
                    throw new BusinessException("Esta socia no tiene deuda pendiente.");

                if (dto.Amount > pendingDebt)
                    throw new BusinessException(
                        $"El abono (${dto.Amount:N0}) supera la deuda pendiente (${pendingDebt:N0}).");
            }

            // Validar SaleId si se proporciona
            if (dto.SaleId.HasValue)
            {
                var sale = await _unitOfWork.Sales.GetByIdAsync(dto.SaleId.Value);
                if (sale == null)
                    throw new NotFoundException("Sale", dto.SaleId.Value);
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

            // BUG 2 FIX: Si el abono tiene SaleId, actualizar el crédito asociado
            if (dto.SaleId.HasValue && type == LiquidationType.Payment)
            {
                var credits = await _unitOfWork.Credits.FindAsync(c => 
                    c.SaleId == dto.SaleId.Value && c.IsActive);
                var credit = credits.FirstOrDefault();
                
                if (credit != null)
                {
                    credit.PaidAmount += dto.Amount;
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
                    
                    var remaining = dto.Amount;
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
            }

            return new PartnerLiquidationDto
            {
                Id = liquidation.Id,
                Amount = liquidation.Amount,
                Type = liquidation.Type.ToString(),
                Date = liquidation.Date,
                Notes = liquidation.Notes,
                PeriodFrom = liquidation.PeriodFrom,
                PeriodTo = liquidation.PeriodTo
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

            // Determinar porcentaje según tipo de producto
            var commissionPercent = product.IsPartnership
                ? partnerConfig.AllianceCommissionPercent
                : partnerConfig.CommissionPercent;

            // BUG 1 FIX: Fórmula correcta partnerPrice = SalePrice - (gainAS × commissionPercent%)
            var gainAS = product.SalePrice - product.Cost;
            return product.SalePrice - (gainAS * commissionPercent / 100);
        }

        public async Task<IEnumerable<PartnerProductViewDto>> GetMyProductsAsync(int partnerConfigId)
        {
            var partnerConfig = await _unitOfWork.PartnerConfigs.GetByIdAsync(partnerConfigId);
            if (partnerConfig == null)
                throw new NotFoundException("PartnerConfig", partnerConfigId);

            var (products, _) = await _unitOfWork.Products.GetAllPagedAsync(
                null, null, null, 1, 1000);

            var result = new List<PartnerProductViewDto>();
            foreach (var product in products)
            {
                // Determinar porcentaje según tipo de producto
                var commissionPercent = product.IsPartnership
                    ? partnerConfig.AllianceCommissionPercent
                    : partnerConfig.CommissionPercent;

                // BUG 1 FIX: Fórmula correcta partnerPrice = SalePrice - (gainAS × commissionPercent%)
                var gainAS = product.SalePrice - product.Cost;
                var partnerPrice = product.SalePrice - (gainAS * commissionPercent / 100);

                result.Add(new PartnerProductViewDto
                {
                    ProductId = product.Id,
                    Code = product.Code,
                    Name = product.Name,
                    CategoryName = product.Category?.Name ?? "",
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

            return liquidations.Select(l => new PartnerLiquidationDto
            {
                Id = l.Id,
                Amount = l.Amount,
                Type = l.Type.ToString(),
                Date = l.Date,
                Notes = l.Notes,
                PeriodFrom = l.PeriodFrom,
                PeriodTo = l.PeriodTo
            });
        }

        public async Task<byte[]> GeneratePartnerStatementAsync(
            int partnerConfigId,
            DateTime from,
            DateTime to)
        {
            return await _partnerReportService
                .GeneratePartnerStatementPdfAsync(partnerConfigId, from, to);
        }

        public async Task<PartnerAdminSummaryDto> GetAdminSummaryAsync(int partnerConfigId)
        {
            var config = await _unitOfWork.PartnerConfigs.GetByIdAsync(partnerConfigId);
            if (config == null)
                throw new NotFoundException("PartnerConfig", partnerConfigId);

            var user = await _unitOfWork.Users.GetByIdAsync(config.UserId);
            
            // BUG 1 FIX: Calcular deuda correctamente (solo créditos pendientes)
            var (totalDebt, totalPaid) = await CalculatePartnerDebtAsync(config.UserId);
            
            var sales = await _unitOfWork.PartnerSales
                .FindAsync(ps => ps.PartnerConfigId == partnerConfigId);

            var invoiceCount = sales.Select(s => s.SaleId).Distinct().Count();

            return new PartnerAdminSummaryDto
            {
                PartnerId = partnerConfigId,
                PartnerName = user?.FullName ?? "",
                TotalDebt = totalDebt,
                TotalPaid = totalPaid,
                PendingDebt = totalDebt - totalPaid,
                InvoiceCount = invoiceCount,
                CommissionPercent = config.CommissionPercent,
                AllianceCommissionPercent = config.AllianceCommissionPercent
            };
        }

        // BUG 1 FIX: Método auxiliar para calcular deuda correcta
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

        public async Task<PagedResponseDto<PartnerInvoiceDto>> GetPartnerInvoicesAsync(
            int partnerConfigId, int pageNumber, int pageSize)
        {
            var config = await _unitOfWork.PartnerConfigs.GetByIdAsync(partnerConfigId);
            if (config == null)
                throw new NotFoundException("PartnerConfig", partnerConfigId);

            // BUG 2 FIX: Obtener ventas con PaymentMethodEntity y Credit incluidos
            var (sales, totalRecords) = await _unitOfWork.Sales.GetSalesWithDetailsAsync(
                config.UserId, "Partner", null, null, null, pageNumber, pageSize);

            var invoices = new List<PartnerInvoiceDto>();
            foreach (var sale in sales)
            {
                var creditStatus = "NoCredit";
                decimal pendingAmount = 0;

                if (sale.Credit != null)
                {
                    creditStatus = sale.Credit.Status.ToString();
                    pendingAmount = sale.Credit.PendingAmount;
                }

                invoices.Add(new PartnerInvoiceDto
                {
                    SaleId = sale.Id,
                    SaleNumber = sale.SaleNumber,
                    Date = sale.Date,
                    Total = sale.Total,
                    PaymentMethodName = sale.PaymentMethodEntity?.Name ?? "Contado",
                    CreditStatus = creditStatus,
                    PendingAmount = pendingAmount
                });
            }

            return new PagedResponseDto<PartnerInvoiceDto>
            {
                Data = invoices,
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
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