using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusAs.Api.Responses;
using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Credits;
using NexusAs.Application.Interfaces;

namespace NexusAs.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class CreditController : ControllerBase
    {
        private readonly ICreditService _creditService;

        public CreditController(ICreditService creditService)
        {
            _creditService = creditService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var credits = await _creditService.GetAllAsync(status, search, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResponseDto<CreditDto>>.Success(credits));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var credit = await _creditService.GetByIdAsync(id);
            return Ok(ApiResponse<CreditDto>.Success(credit!));
        }

        [HttpPost("{id}/payment")]
        public async Task<IActionResult> RegisterPayment(int id, [FromBody] PaymentDto dto)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var credit = await _creditService.RegisterPaymentAsync(id, dto, userId);
            return Ok(ApiResponse<CreditDto>.Success(credit, "Abono registrado exitosamente."));
        }

        [HttpGet("{id}/details")]
        public async Task<IActionResult> GetFullDetails(int id)
        {
            var detail = await _creditService.GetFullDetailsAsync(id);
            return Ok(ApiResponse<CreditDetailDto>.Success(detail));
        }
    }
}