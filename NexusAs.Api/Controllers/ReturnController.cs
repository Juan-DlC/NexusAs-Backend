using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusAs.Api.Responses;
using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Returns;
using NexusAs.Application.Interfaces;
using System.Security.Claims;

namespace NexusAs.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReturnController : ControllerBase
    {
        private readonly IReturnService _returnService;
        private readonly ILogger<ReturnController> _logger;

        public ReturnController(IReturnService returnService, ILogger<ReturnController> logger)
        {
            _returnService = returnService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene lista paginada de devoluciones con filtros de fecha
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var returns = await _returnService.GetAllAsync(pageNumber, pageSize, from, to);
            return Ok(ApiResponse<PagedResponseDto<ReturnDto>>.Success(returns));
        }

        /// <summary>
        /// Obtiene una devolución por ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var returnEntity = await _returnService.GetByIdAsync(id);
            return Ok(ApiResponse<ReturnDto>.Success(returnEntity!));
        }

        /// <summary>
        /// Crea una nueva devolución (procesa automáticamente stock, créditos y ganancias de socias)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> Create([FromBody] CreateReturnDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(ApiResponse<string>.Fail("Usuario no autenticado."));

                var returnEntity = await _returnService.CreateAsync(dto, userId);
            _logger.LogInformation("Devolución procesada para venta {SaleId} por usuario {UserId}. Monto: ${TotalReturned:N0}",
                dto.SaleId, userId, returnEntity.TotalAmount);
            return CreatedAtAction(nameof(GetById),
                new { id = returnEntity.Id },
                ApiResponse<ReturnDto>.Success(returnEntity, "Devolución procesada exitosamente."));
        }

        /// <summary>
        /// Obtiene todas las devoluciones de una venta específica
        /// </summary>
        [HttpGet("by-sale/{saleId}")]
        [Authorize]
        public async Task<IActionResult> GetBySaleId(int saleId)
        {
            var returns = await _returnService.GetBySaleIdAsync(saleId);
            return Ok(ApiResponse<IEnumerable<ReturnDto>>.Success(returns));
        }
    }
}
