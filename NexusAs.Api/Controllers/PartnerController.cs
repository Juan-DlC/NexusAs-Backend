using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusAs.Api.Responses;
using NexusAs.Application.DTOs.Partners;
using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Sales;
using NexusAs.Application.Interfaces;
using System.Security.Claims;

namespace NexusAs.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PartnerController : ControllerBase
    {
        private readonly IPartnerService _partnerService;

        public PartnerController(IPartnerService partnerService)
        {
            _partnerService = partnerService;
        }

        [HttpGet("alliance-report")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllianceReport(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to)
        {
            var report = await _partnerService.GetAllianceReportAsync(from, to);
            return Ok(ApiResponse<AllianceReportDto>.Success(report));
        }

        [HttpGet("my/products")]
        [Authorize(Roles = "Partner")]
        public async Task<IActionResult> GetMyProducts()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var config = await GetPartnerConfigByUserId(userId);
            var products = await _partnerService.GetMyProductsAsync(config.Id);
            return Ok(ApiResponse<IEnumerable<PartnerProductViewDto>>.Success(products));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var partners = await _partnerService.GetAllPartnersAsync();
            return Ok(ApiResponse<IEnumerable<PartnerConfigDto>>.Success(partners));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var partner = await _partnerService.GetPartnerByIdAsync(id);
            return Ok(ApiResponse<PartnerConfigDto>.Success(partner!));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreatePartnerConfigDto dto)
        {
            var partner = await _partnerService.CreatePartnerConfigAsync(dto);
            return Ok(ApiResponse<PartnerConfigDto>.Success(
                partner, "Configuración de socia creada exitosamente."));
        }

        [HttpPatch("{id}/commission")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCommission(
            int id, [FromBody] UpdatePartnerCommissionDto dto)
        {
            var partner = await _partnerService.UpdateCommissionAsync(id, dto);
            return Ok(ApiResponse<PartnerConfigDto>.Success(
                partner, "Comisiones actualizadas exitosamente."));
        }

        [HttpPost("{id}/product-prices")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetProductPrice(
            int id, [FromBody] SetPartnerProductPriceDto dto)
        {
            var price = await _partnerService.SetProductPriceAsync(id, dto);
            return Ok(ApiResponse<PartnerProductPriceDto>.Success(
                price, "Precio especial configurado exitosamente."));
        }

        [HttpGet("{id}/product-prices")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetProductPrices(int id)
        {
            var prices = await _partnerService.GetProductPricesAsync(id);
            return Ok(ApiResponse<IEnumerable<PartnerProductPriceDto>>.Success(prices));
        }

        [HttpGet("{id}/summary")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSummaryAdmin(int id)
        {
            var summary = await _partnerService.GetPartnerSummaryAsync(id);
            return Ok(ApiResponse<PartnerSummaryDto>.Success(summary));
        }

        [HttpGet("{id}/admin-summary")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminSummary(int id)
        {
            var summary = await _partnerService.GetAdminSummaryAsync(id);
            return Ok(ApiResponse<PartnerAdminSummaryDto>.Success(summary));
        }

        [HttpGet("{id}/invoices")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPartnerInvoices(
            int id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var invoices = await _partnerService.GetPartnerInvoicesAsync(id, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResponseDto<PartnerInvoiceDto>>.Success(invoices));
        }

        [HttpGet("{id}/invoices/{saleId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPartnerInvoiceDetail(int id, int saleId)
        {
            // Validar que la venta pertenece a la socia
            var config = await _partnerService.GetPartnerByIdAsync(id);
            if (config == null)
                return NotFound(ApiResponse<string>.Fail($"Configuración de socia {id} no encontrada."));

            var sale = await _partnerService.GetPartnerInvoiceDetailAsync(config.UserId, saleId);
            return Ok(ApiResponse<SaleDto>.Success(sale));
        }

        [HttpGet("{id}/sales")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSalesAdmin(
            int id,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var sales = await _partnerService.GetPartnerSalesAsync(id, from, to);
            return Ok(ApiResponse<IEnumerable<PartnerSaleDto>>.Success(sales));
        }

        [HttpPost("{id}/liquidations")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegisterLiquidation(
            int id, [FromBody] RegisterPartnerLiquidationDto dto)
        {
            var liquidation = await _partnerService.RegisterLiquidationAsync(id, dto);
            return Ok(ApiResponse<PartnerLiquidationDto>.Success(
                liquidation, "Liquidación registrada exitosamente."));
        }

        [HttpGet("{id}/liquidations")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetLiquidations(int id)
        {
            var liquidations = await _partnerService.GetLiquidationsAsync(id);
            return Ok(ApiResponse<IEnumerable<PartnerLiquidationDto>>.Success(liquidations));
        }

        // TAREA 3: Endpoint con paginación para liquidaciones
        [HttpGet("{id}/liquidations/paged")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetLiquidationsPaged(
            int id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _partnerService.GetLiquidationsPagedAsync(id, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResponseDto<PartnerLiquidationDto>>.Success(result));
        }

        [HttpGet("{id}/statement/pdf")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStatementAdmin(
            int id,
            [FromQuery] DateTime from,
            [FromQuery] DateTime to)
        {
            var pdf = await _partnerService.GeneratePartnerStatementAsync(id, from, to);
            return File(pdf, "application/pdf", $"EstadoCuenta_Socia_{id}.pdf");
        }

        // ── PARTNER endpoints (la socia ve los suyos) ───

        [HttpGet("my/summary")]
        [Authorize(Roles = "Partner")]
        public async Task<IActionResult> GetMySummary()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var config = await GetPartnerConfigByUserId(userId);
            var summary = await _partnerService.GetPartnerSummaryAsync(config.Id);
            return Ok(ApiResponse<PartnerSummaryDto>.Success(summary));
        }

        [HttpGet("my/sales")]
        [Authorize(Roles = "Partner")]
        public async Task<IActionResult> GetMySales(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var config = await GetPartnerConfigByUserId(userId);
            var sales = await _partnerService.GetPartnerSalesAsync(config.Id, from, to);
            return Ok(ApiResponse<IEnumerable<PartnerSaleDto>>.Success(sales));
        }

        [HttpGet("my/statement/pdf")]
        [Authorize(Roles = "Partner")]
        public async Task<IActionResult> GetMyStatement(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var config = await GetPartnerConfigByUserId(userId);
            var pdf = await _partnerService
                .GeneratePartnerStatementAsync(config.Id, from, to);
            return File(pdf, "application/pdf", "MiEstadoCuenta.pdf");
        }

        // ── Helper privado ───────────────────────────────
        private async Task<PartnerConfigDto> GetPartnerConfigByUserId(int userId)
        {
            var all = await _partnerService.GetAllPartnersAsync();
            var config = all.FirstOrDefault(p => p.UserId == userId && p.IsActive);
            if (config == null)
                throw new Exception("No se encontró configuración de socia para este usuario.");
            return config;
        }
    }
}