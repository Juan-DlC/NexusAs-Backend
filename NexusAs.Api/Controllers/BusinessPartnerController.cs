using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusAs.Api.Responses;
using NexusAs.Application.DTOs.BusinessPartners;
using NexusAs.Application.DTOs.Common;
using NexusAs.Application.Interfaces;
using System.Security.Claims;

namespace NexusAs.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BusinessPartnerController : ControllerBase
    {
        private readonly IBusinessPartnerService _service;
        private readonly IBusinessPartnerReportService _reportService;
        private readonly ILogger<BusinessPartnerController> _logger;

        public BusinessPartnerController(
            IBusinessPartnerService service,
            IBusinessPartnerReportService reportService,
            ILogger<BusinessPartnerController> logger)
        {
            _service = service;
            _reportService = reportService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResponseDto<BusinessPartnerDto>>>> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActiveFilter = null)
        {
            var result = await _service.GetAllAsync(pageNumber, pageSize, search, isActiveFilter);
            return Ok(ApiResponse<PagedResponseDto<BusinessPartnerDto>>.Success(result));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<BusinessPartnerDto>>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(ApiResponse<BusinessPartnerDto>.Success(result!));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<BusinessPartnerDto>>> Create(
            [FromBody] CreateBusinessPartnerDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(
                nameof(GetById), 
                new { id = result.Id }, 
                ApiResponse<BusinessPartnerDto>.Success(result, "Socio comercial creado exitosamente"));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<BusinessPartnerDto>>> Update(
            int id, 
            [FromBody] UpdateBusinessPartnerDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(ApiResponse<BusinessPartnerDto>.Success(result, "Socio comercial actualizado exitosamente"));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object?>>> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return Ok(ApiResponse<object?>.Success(null, "Socio comercial eliminado exitosamente"));
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/toggle-status")]
        public async Task<ActionResult<ApiResponse<BusinessPartnerDto>>> ToggleStatus(int id)
        {
            var result = await _service.ToggleStatusAsync(id);
            var message = result.IsActive 
                ? "Socio comercial activado exitosamente" 
                : "Socio comercial desactivado exitosamente";
            return Ok(ApiResponse<BusinessPartnerDto>.Success(result, message));
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/commission")]
        public async Task<ActionResult<ApiResponse<BusinessPartnerDto>>> UpdateCommission(
            int id,
            [FromBody] UpdateCommissionDto dto)
        {
            var result = await _service.UpdateCommissionAsync(id, dto);
            return Ok(ApiResponse<BusinessPartnerDto>.Success(result, "Comisión actualizada exitosamente"));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{businessPartnerId}/liquidation/preview")]
        public async Task<ActionResult<ApiResponse<BusinessPartnerLiquidationPreviewDto>>> PreviewLiquidation(
            int businessPartnerId,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            // Permitir tanto fromDate/toDate como from/to para compatibilidad con frontend
            var startDate = fromDate ?? from ?? DateTime.Today.AddMonths(-1);
            var endDate = toDate ?? to ?? DateTime.Today.AddDays(1).AddSeconds(-1);
            
            var result = await _service.PreviewLiquidationAsync(businessPartnerId, startDate, endDate);
            return Ok(ApiResponse<BusinessPartnerLiquidationPreviewDto>.Success(result));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{businessPartnerId}/liquidation/confirm")]
        public async Task<ActionResult<ApiResponse<BusinessPartnerLiquidationDto>>> ConfirmLiquidation(
            int businessPartnerId,
            [FromBody] ConfirmLiquidationRequestDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _service.ConfirmLiquidationAsync(
                businessPartnerId, 
                dto.FromDate, 
                dto.ToDate, 
                userId, 
                dto.Notes);
            
            _logger.LogInformation("Liquidación confirmada para socio {BusinessPartnerId} por usuario {UserId}. Monto total: ${TotalAmount:N0}",
                businessPartnerId, userId, result.TotalAmount);
            
            return Ok(ApiResponse<BusinessPartnerLiquidationDto>.Success(
                result, 
                "Liquidación confirmada exitosamente"));
        }

        [HttpGet("liquidations")]
        public async Task<ActionResult<ApiResponse<PagedResponseDto<BusinessPartnerLiquidationDto>>>> GetLiquidations(
            [FromQuery] int? businessPartnerId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetLiquidationsAsync(
                businessPartnerId, 
                fromDate, 
                toDate, 
                pageNumber, 
                pageSize);
            
            return Ok(ApiResponse<PagedResponseDto<BusinessPartnerLiquidationDto>>.Success(result));
        }

        // Endpoint alternativo para obtener liquidaciones de un socio específico
        [HttpGet("{businessPartnerId}/liquidations")]
        public async Task<ActionResult<ApiResponse<PagedResponseDto<BusinessPartnerLiquidationDto>>>> GetLiquidationsByBusinessPartner(
            int businessPartnerId,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetLiquidationsAsync(
                businessPartnerId, 
                fromDate, 
                toDate, 
                pageNumber, 
                pageSize);
            
            return Ok(ApiResponse<PagedResponseDto<BusinessPartnerLiquidationDto>>.Success(result));
        }

        [HttpGet("liquidations/{id}")]
        public async Task<ActionResult<ApiResponse<BusinessPartnerLiquidationDto>>> GetLiquidationById(int id)
        {
            var result = await _service.GetLiquidationByIdAsync(id);
            if (result == null)
                return NotFound(ApiResponse<BusinessPartnerLiquidationDto>.Fail("Liquidación no encontrada"));
            return Ok(ApiResponse<BusinessPartnerLiquidationDto>.Success(result));
        }

        [HttpGet("liquidations/{liquidationId}/pdf")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetLiquidationPdf(int liquidationId)
        {
            var pdfBytes = await _reportService.GenerateLiquidationPdfAsync(liquidationId);
            return File(pdfBytes, "application/pdf", $"liquidacion-{liquidationId}.pdf");
        }
    }

    public class ConfirmLiquidationRequestDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? Notes { get; set; }
    }
}
