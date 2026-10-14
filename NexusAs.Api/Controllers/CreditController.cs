using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusAs.Api.Extensions;
using NexusAs.Api.Responses;
using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Credits;
using NexusAs.Application.Interfaces;

namespace NexusAs.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CreditController : ControllerBase
    {
        private readonly ICreditService _creditService;

        public CreditController(ICreditService creditService)
        {
            _creditService = creditService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Partner")]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = User.GetUserIdOrThrow();
            var userRole = User.GetUserRoleOrThrow();
            var credits = await _creditService.GetAllAsync(pageNumber, pageSize, search, status, userId, userRole);
            return Ok(ApiResponse<PagedResponseDto<CreditDto>>.Success(credits));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var credit = await _creditService.GetByIdAsync(id);
            return Ok(ApiResponse<CreditDto>.Success(credit!));
        }

        [HttpPost("{id}/payment")]
        [Authorize(Roles = "Admin,Partner")]
        public async Task<IActionResult> RegisterPayment(int id, [FromBody] PaymentDto dto)
        {
            var userId = User.GetUserIdOrThrow();
            var userRole = User.GetUserRoleOrThrow();
            var credit = await _creditService.RegisterPaymentAsync(id, dto, userId, userRole);
            return Ok(ApiResponse<CreditDto>.Success(credit, "Abono registrado exitosamente."));
        }

        [HttpGet("{id}/details")]
        [Authorize(Roles = "Admin,Partner")]
        public async Task<IActionResult> GetFullDetails(int id)
        {
            var detail = await _creditService.GetFullDetailsAsync(id);
            return Ok(ApiResponse<CreditDetailDto>.Success(detail));
        }
    }
}