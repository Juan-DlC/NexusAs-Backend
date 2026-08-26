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

        public BusinessPartnerController(IBusinessPartnerService service)
        {
            _service = service;
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
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return Ok(ApiResponse<object>.Success(null, "Socio comercial eliminado exitosamente"));
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
        [HttpGet("{businessPartnerId}/liquidation/preview")]
        public async Task<ActionResult<ApiResponse<BusinessPartnerLiquidationPreviewDto>>> PreviewLiquidation(
            int businessPartnerId,
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            var result = await _service.PreviewLiquidationAsync(businessPartnerId, fromDate, toDate);
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

        [HttpGet("liquidations/{id}")]
        public async Task<ActionResult<ApiResponse<BusinessPartnerLiquidationDto>>> GetLiquidationById(int id)
        {
            var result = await _service.GetLiquidationByIdAsync(id);
            return Ok(ApiResponse<BusinessPartnerLiquidationDto>.Success(result!));
        }
    }

    public class ConfirmLiquidationRequestDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? Notes { get; set; }
    }
}
