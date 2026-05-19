using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusAs.Api.Responses;
using NexusAs.Application.DTOs.Stock;
using NexusAs.Application.Interfaces;
using System.Security.Claims;

namespace NexusAs.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;

        public StockController(IStockService stockService)
        {
            _stockService = stockService;
        }

        [HttpGet("{productId}/movements")]
        public async Task<IActionResult> GetMovements(int productId)
        {
            var movements = await _stockService.GetMovementsByProductAsync(productId);
            return Ok(ApiResponse<IEnumerable<StockMovementDto>>.Success(movements));
        }

        [HttpPost("entry")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegisterEntry([FromBody] StockEntryDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var movement = await _stockService.RegisterEntryAsync(dto, userId);
            return Ok(ApiResponse<StockMovementDto>.Success(
                movement, "Entrada de stock registrada exitosamente."));
        }

        [HttpPost("adjustment")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegisterAdjustment([FromBody] StockAdjustmentDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var movement = await _stockService.RegisterAdjustmentAsync(dto, userId);
            return Ok(ApiResponse<StockMovementDto>.Success(
                movement, "Ajuste de stock registrado exitosamente."));
        }
    }
}