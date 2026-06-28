    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using NexusAs.Api.Responses;
using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Sales;
    using NexusAs.Application.Interfaces;
    using NexusAs.Infrastructure.Services;
    using System.Security.Claims;

    namespace NexusAs.Api.Controllers
    {
        [ApiController]
        [Route("api/[controller]")]
        [Authorize]
        public class SaleController : ControllerBase
        {
            private readonly ISaleService _saleService;
            private readonly IReportService _reportService;

            public SaleController(ISaleService saleService, IReportService reportService)
            {
                _saleService = saleService;
                _reportService = reportService;
            }

            [HttpGet]
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? search = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userRole = User.FindFirst(ClaimTypes.Role)!.Value;
            var sales = await _saleService.GetAllAsync(userId, userRole, from, to, search, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResponseDto<SaleDto>>.Success(sales));
        }

        [HttpGet("{id}")]
            public async Task<IActionResult> GetById(int id)
            {
                var sale = await _saleService.GetByIdAsync(id);
                return Ok(ApiResponse<SaleDto>.Success(sale!));
            }

            [HttpGet("{id}/receipt")]
            public async Task<IActionResult> GetReceipt(int id)
            {
                var pdf = await _reportService.GenerateSaleReceiptAsync(id);
                return File(pdf, "application/pdf", $"Recibo_{id}.pdf");
            }

            [HttpPost]
            public async Task<IActionResult> Create([FromBody] CreateSaleDto dto)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var sale = await _saleService.CreateAsync(dto, userId);
                return CreatedAtAction(nameof(GetById),
                    new { id = sale.Id },
                    ApiResponse<SaleDto>.Success(sale, "Venta registrada exitosamente."));
            }
        }
    }