using AutoMapper;
using NexusAs.Application.DTOs.BusinessPartners;
using NexusAs.Application.DTOs.Common;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Entities;
using NexusAs.Domain.Exceptions;

namespace NexusAs.Application.Services
{
    public class BusinessPartnerService : IBusinessPartnerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public BusinessPartnerService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<PagedResponseDto<BusinessPartnerDto>> GetAllAsync(
            int pageNumber = 1, 
            int pageSize = 10, 
            string? search = null, 
            bool? isActiveFilter = null)
        {
            var (businessPartners, totalRecords) = await _unitOfWork.BusinessPartners
                .GetAllPagedWithProductsAsync(pageNumber, pageSize, search, isActiveFilter);

            return new PagedResponseDto<BusinessPartnerDto>
            {
                Data = _mapper.Map<IEnumerable<BusinessPartnerDto>>(businessPartners),
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<BusinessPartnerDto?> GetByIdAsync(int id)
        {
            var businessPartner = await _unitOfWork.BusinessPartners.GetByIdWithProductsAsync(id);

            if (businessPartner == null)
                throw new NotFoundException(nameof(BusinessPartner), id);

            return _mapper.Map<BusinessPartnerDto>(businessPartner);
        }

        public async Task<BusinessPartnerDto> CreateAsync(CreateBusinessPartnerDto dto)
        {
            var exists = await _unitOfWork.BusinessPartners
                .ExistsAsync(bp => bp.DocumentNumber == dto.DocumentNumber && bp.IsActive);
            
            if (exists)
                throw new BusinessException(
                    $"Ya existe un socio comercial con el documento '{dto.DocumentNumber}'.");

            if (dto.CommissionPercent < 0 || dto.CommissionPercent > 100)
                throw new BusinessException(
                    "El porcentaje de comisión debe estar entre 0 y 100.");

            var businessPartner = _mapper.Map<BusinessPartner>(dto);
            await _unitOfWork.BusinessPartners.AddAsync(businessPartner);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<BusinessPartnerDto>(businessPartner);
        }

        public async Task<BusinessPartnerDto> UpdateAsync(int id, UpdateBusinessPartnerDto dto)
        {
            var businessPartner = await _unitOfWork.BusinessPartners.GetByIdAsync(id);
            if (businessPartner == null)
                throw new NotFoundException(nameof(BusinessPartner), id);

            var exists = await _unitOfWork.BusinessPartners
                .ExistsAsync(bp => bp.DocumentNumber == dto.DocumentNumber && 
                    bp.Id != id && bp.IsActive);
            
            if (exists)
                throw new BusinessException(
                    $"Ya existe un socio comercial con el documento '{dto.DocumentNumber}'.");

            if (dto.CommissionPercent < 0 || dto.CommissionPercent > 100)
                throw new BusinessException(
                    "El porcentaje de comisión debe estar entre 0 y 100.");

            _mapper.Map(dto, businessPartner);
            _unitOfWork.BusinessPartners.Update(businessPartner);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<BusinessPartnerDto>(businessPartner);
        }

        public async Task DeleteAsync(int id)
        {
            var businessPartner = await _unitOfWork.BusinessPartners.GetByIdAsync(id);
            if (businessPartner == null)
                throw new NotFoundException(nameof(BusinessPartner), id);

            // Validar que no tenga productos asociados activos
            var hasActiveProducts = await _unitOfWork.Products
                .ExistsAsync(p => p.BusinessPartnerId == id && p.IsActive);
            
            if (hasActiveProducts)
                throw new BusinessException(
                    "No se puede eliminar el socio comercial porque tiene productos asociados activos.");

            _unitOfWork.BusinessPartners.Delete(businessPartner);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<BusinessPartnerDto> ToggleStatusAsync(int id)
        {
            var businessPartner = await _unitOfWork.BusinessPartners.GetByIdAsync(id);
            
            if (businessPartner == null)
                throw new NotFoundException(nameof(BusinessPartner), id);

            businessPartner.IsActive = !businessPartner.IsActive;
            _unitOfWork.BusinessPartners.Update(businessPartner);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<BusinessPartnerDto>(businessPartner);
        }

        public async Task<BusinessPartnerLiquidationPreviewDto> PreviewLiquidationAsync(
            int businessPartnerId, 
            DateTime fromDate, 
            DateTime toDate)
        {
            var businessPartner = await _unitOfWork.BusinessPartners.GetByIdAsync(businessPartnerId);

            if (businessPartner == null || !businessPartner.IsActive)
                throw new NotFoundException(nameof(BusinessPartner), businessPartnerId);

            // Obtener todas las ventas de productos asociados al socio comercial en el rango de fechas
            // NOTA: Las ventas ya liquidadas se excluyen automáticamente en el repositorio
            var sales = await _unitOfWork.BusinessPartners
                .GetSaleDetailsByBusinessPartnerAndDateRangeAsync(businessPartnerId, fromDate, toDate);

            // Si no hay ventas disponibles, devolver preview vacío (sin error)
            if (!sales.Any())
            {
                return new BusinessPartnerLiquidationPreviewDto
                {
                    BusinessPartnerId = businessPartnerId,
                    BusinessPartnerName = businessPartner.Name,
                    CommissionPercent = businessPartner.CommissionPercent,
                    FromDate = fromDate,
                    ToDate = toDate,
                    Sales = new List<LiquidationSaleDetailDto>(),
                    TotalGrossProfit = 0,
                    TotalPartnerCommission = 0,
                    TotalRemainingProfit = 0,
                    TotalBusinessPartnerAmount = 0,
                    TotalAsAmount = 0,
                    TotalSales = 0
                };
            }

            // Calcular detalles de liquidación
            var details = new List<LiquidationSaleDetailDto>();

            foreach (var saleDetail in sales)
            {
                // FÓRMULA CORRECTA:
                // 1. GananciaBruta = SalePrice - Cost
                var grossProfit = (saleDetail.Product.SalePrice - saleDetail.Product.Cost) * saleDetail.Quantity;

                // 2. Determinar comisión para socias vendedoras
                var partnerCommissionPercent = 0m;
                var partnerCommissionAmount = 0m;

                // Si la venta fue hecha por una socia (Partner role)
                if (saleDetail.Sale.User.Role == Domain.Enums.UserRole.Partner)
                {
                    var partnerConfigs = await _unitOfWork.PartnerConfigs
                        .FindAsync(pc => pc.UserId == saleDetail.Sale.UserId && pc.IsActive);
                    var partnerConfig = partnerConfigs.FirstOrDefault();

                    if (partnerConfig != null)
                    {
                        // Para productos en alianza, usar AllianceCommissionPercent
                        // IMPORTANTE: NO usar BusinessPartner.CommissionPercent aquí
                        partnerCommissionPercent = partnerConfig.AllianceCommissionPercent;
                        partnerCommissionAmount = grossProfit * partnerCommissionPercent / 100;
                    }
                }

                // 3. GananciaRestante = GananciaBruta - GananciaParaSocias
                var remainingProfit = grossProfit - partnerCommissionAmount;

                // 4. LeCorrespondeAlSocioComercial = GananciaRestante × BusinessPartner.CommissionPercent/100
                var businessPartnerCommissionPercent = businessPartner.CommissionPercent;
                var businessPartnerAmount = remainingProfit * businessPartnerCommissionPercent / 100;

                // 5. LeCorrespondeAS = GananciaRestante - LeCorrespondeAlSocioComercial
                var asAmount = remainingProfit - businessPartnerAmount;

                details.Add(new LiquidationSaleDetailDto
                {
                    SaleId = saleDetail.SaleId,
                    SaleNumber = saleDetail.Sale.SaleNumber,
                    SaleDate = saleDetail.Sale.Date,
                    SellerName = saleDetail.Sale.User?.FullName ?? "Sin vendedor",
                    PaymentMethodName = saleDetail.Sale.PaymentMethodEntity?.Name ?? "Contado",
                    ProductId = saleDetail.ProductId,
                    ProductCode = saleDetail.Product.Code,
                    ProductName = saleDetail.Product.Name,
                    Quantity = saleDetail.Quantity,
                    CostPrice = saleDetail.Product.Cost,
                    SalePrice = saleDetail.Product.SalePrice,
                    GrossProfit = Math.Round(grossProfit, 2),
                    PartnerCommissionPercent = partnerCommissionPercent,
                    PartnerCommissionAmount = Math.Round(partnerCommissionAmount, 2),
                    RemainingProfit = Math.Round(remainingProfit, 2),
                    BusinessPartnerCommissionPercent = businessPartnerCommissionPercent,
                    BusinessPartnerAmount = Math.Round(businessPartnerAmount, 2),
                    AsAmount = Math.Round(asAmount, 2)
                });
            }

            return new BusinessPartnerLiquidationPreviewDto
            {
                BusinessPartnerId = businessPartnerId,
                BusinessPartnerName = businessPartner.Name,
                CommissionPercent = businessPartner.CommissionPercent,
                FromDate = fromDate,
                ToDate = toDate,
                Sales = details.OrderBy(d => d.SaleDate).ToList(),
                TotalGrossProfit = details.Sum(d => d.GrossProfit),
                TotalPartnerCommission = details.Sum(d => d.PartnerCommissionAmount),
                TotalRemainingProfit = details.Sum(d => d.RemainingProfit),
                TotalBusinessPartnerAmount = details.Sum(d => d.BusinessPartnerAmount),
                TotalAsAmount = details.Sum(d => d.AsAmount),
                TotalSales = details.Select(d => d.SaleId).Distinct().Count()
            };
        }

        public async Task<BusinessPartnerLiquidationDto> ConfirmLiquidationAsync(
            int businessPartnerId, 
            DateTime fromDate, 
            DateTime toDate, 
            int processedByUserId, 
            string? notes = null)
        {
            // Primero obtener el preview para validar y usar los cálculos
            var preview = await PreviewLiquidationAsync(businessPartnerId, fromDate, toDate);

            if (!preview.Sales.Any())
                throw new BusinessException("No hay ventas para liquidar en el rango de fechas seleccionado.");

            // Generar número de liquidación
            var lastLiquidation = await _unitOfWork.BusinessPartners.GetLastLiquidationNumberAsync();

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastLiquidation))
            {
                var parts = lastLiquidation.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[1], out int last))
                    nextNumber = last + 1;
            }
            var liquidationNumber = $"LIQ-{nextNumber:D4}";

            // Crear la liquidación
            var liquidation = new BusinessPartnerLiquidation
            {
                LiquidationNumber = liquidationNumber,
                BusinessPartnerId = businessPartnerId,
                LiquidationDate = DateTime.Now,
                FromDate = fromDate,
                ToDate = toDate,
                TotalAmount = preview.TotalBusinessPartnerAmount,
                Notes = notes,
                ProcessedByUserId = processedByUserId
            };

            await _unitOfWork.BusinessPartnerLiquidations.AddAsync(liquidation);
            await _unitOfWork.SaveChangesAsync();

            // Crear los detalles de liquidación
            foreach (var saleDetail in preview.Sales)
            {
                var detail = new BusinessPartnerLiquidationDetail
                {
                    LiquidationId = liquidation.Id,
                    SaleId = saleDetail.SaleId,
                    ProductId = saleDetail.ProductId,
                    Quantity = saleDetail.Quantity,
                    CostPrice = saleDetail.CostPrice,
                    SalePrice = saleDetail.SalePrice,
                    GrossProfit = saleDetail.GrossProfit,
                    PartnerCommissionPercent = saleDetail.PartnerCommissionPercent,
                    BusinessPartnerCommissionPercent = saleDetail.BusinessPartnerCommissionPercent,
                    PartnerCommissionAmount = saleDetail.PartnerCommissionAmount,
                    RemainingProfit = saleDetail.RemainingProfit,
                    BusinessPartnerAmount = saleDetail.BusinessPartnerAmount,
                    AsAmount = saleDetail.AsAmount
                };

                await _unitOfWork.BusinessPartnerLiquidationDetails.AddAsync(detail);
            }

            await _unitOfWork.SaveChangesAsync();

            // Retornar el DTO completo
            return await GetLiquidationByIdAsync(liquidation.Id) 
                ?? throw new NotFoundException(nameof(BusinessPartnerLiquidation), liquidation.Id);
        }

        public async Task<PagedResponseDto<BusinessPartnerLiquidationDto>> GetLiquidationsAsync(
            int? businessPartnerId = null, 
            DateTime? fromDate = null, 
            DateTime? toDate = null, 
            int pageNumber = 1, 
            int pageSize = 10)
        {
            var (liquidations, totalRecords) = await _unitOfWork.BusinessPartners
                .GetLiquidationsPagedAsync(businessPartnerId, fromDate, toDate, pageNumber, pageSize);

            var dtos = liquidations.Select(l => new BusinessPartnerLiquidationDto
            {
                Id = l.Id,
                LiquidationNumber = l.LiquidationNumber,
                BusinessPartnerId = l.BusinessPartnerId,
                BusinessPartnerName = l.BusinessPartner.Name,
                LiquidationDate = l.LiquidationDate,
                FromDate = l.FromDate,
                ToDate = l.ToDate,
                TotalAmount = l.TotalAmount,
                Notes = l.Notes,
                ProcessedByUserId = l.ProcessedByUserId,
                ProcessedByUserName = l.ProcessedByUser.FullName,
                IsActive = l.IsActive,
                CreatedAt = l.CreatedAt,
                TotalSales = l.Details.Select(d => d.SaleId).Distinct().Count(),
                Details = l.Details.Select(d => new LiquidationSaleDetailDto
                {
                    SaleId = d.SaleId,
                    SaleNumber = d.Sale.SaleNumber,
                    SaleDate = d.Sale.Date,
                    SellerName = d.Sale.User?.FullName ?? "Sin vendedor",
                    PaymentMethodName = d.Sale.PaymentMethodEntity?.Name ?? "Contado",
                    ProductId = d.ProductId,
                    ProductCode = d.Product.Code,
                    ProductName = d.Product.Name,
                    Quantity = d.Quantity,
                    CostPrice = d.CostPrice,
                    SalePrice = d.SalePrice,
                    GrossProfit = d.GrossProfit,
                    PartnerCommissionPercent = d.PartnerCommissionPercent,
                    PartnerCommissionAmount = d.PartnerCommissionAmount,
                    RemainingProfit = d.RemainingProfit,
                    BusinessPartnerCommissionPercent = d.BusinessPartnerCommissionPercent,
                    BusinessPartnerAmount = d.BusinessPartnerAmount,
                    AsAmount = d.AsAmount
                }).ToList()
            }).ToList();

            return new PagedResponseDto<BusinessPartnerLiquidationDto>
            {
                Data = dtos,
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<BusinessPartnerLiquidationDto?> GetLiquidationByIdAsync(int id)
        {
            var liquidation = await _unitOfWork.BusinessPartners.GetLiquidationByIdWithDetailsAsync(id);

            if (liquidation == null)
                throw new NotFoundException(nameof(BusinessPartnerLiquidation), id);

            return new BusinessPartnerLiquidationDto
            {
                Id = liquidation.Id,
                LiquidationNumber = liquidation.LiquidationNumber,
                BusinessPartnerId = liquidation.BusinessPartnerId,
                BusinessPartnerName = liquidation.BusinessPartner.Name,
                LiquidationDate = liquidation.LiquidationDate,
                FromDate = liquidation.FromDate,
                ToDate = liquidation.ToDate,
                TotalAmount = liquidation.TotalAmount,
                Notes = liquidation.Notes,
                ProcessedByUserId = liquidation.ProcessedByUserId,
                ProcessedByUserName = liquidation.ProcessedByUser.FullName,
                IsActive = liquidation.IsActive,
                CreatedAt = liquidation.CreatedAt,
                TotalSales = liquidation.Details.Select(d => d.SaleId).Distinct().Count(),
                Details = liquidation.Details.Select(d => new LiquidationSaleDetailDto
                {
                    SaleId = d.SaleId,
                    SaleNumber = d.Sale.SaleNumber,
                    SaleDate = d.Sale.Date,
                    SellerName = d.Sale.User?.FullName ?? "Sin vendedor",
                    PaymentMethodName = d.Sale.PaymentMethodEntity?.Name ?? "Contado",
                    ProductId = d.ProductId,
                    ProductCode = d.Product.Code,
                    ProductName = d.Product.Name,
                    Quantity = d.Quantity,
                    CostPrice = d.CostPrice,
                    SalePrice = d.SalePrice,
                    GrossProfit = d.GrossProfit,
                    PartnerCommissionPercent = d.PartnerCommissionPercent,
                    PartnerCommissionAmount = d.PartnerCommissionAmount,
                    RemainingProfit = d.RemainingProfit,
                    BusinessPartnerCommissionPercent = d.BusinessPartnerCommissionPercent,
                    BusinessPartnerAmount = d.BusinessPartnerAmount,
                    AsAmount = d.AsAmount
                }).ToList()
            };
        }
    }
}
