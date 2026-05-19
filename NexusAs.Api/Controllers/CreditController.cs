using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusAs.Api.Responses;
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
        public async Task<IActionResult> GetAll([FromQuery] string? status = null)
        {
            var credits = await _creditService.GetAllAsync(status);
            return Ok(ApiResponse<IEnumerable<CreditDto>>.Success(credits));
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
            var credit = await _creditService.RegisterPaymentAsync(id, dto);
            return Ok(ApiResponse<CreditDto>.Success(credit, "Abono registrado exitosamente."));
        }
    }
}