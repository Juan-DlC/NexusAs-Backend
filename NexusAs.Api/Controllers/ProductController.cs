using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusAs.Api.Responses;
using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Products;
using NexusAs.Application.Interfaces;

namespace NexusAs.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] bool? isPartnership = null,
            [FromQuery] bool? inStock = null,
            [FromQuery] bool? isActive = null)
        {
            var products = await _productService.GetAllAsync(
                pageNumber, pageSize, search, categoryId, isPartnership, inStock, isActive);
            return Ok(ApiResponse<PagedResponseDto<ProductDto>>.Success(products));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            return Ok(ApiResponse<ProductDto>.Success(product!));
        }

        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStock()
        {
            var products = await _productService.GetLowStockAsync();
            return Ok(ApiResponse<IEnumerable<ProductDto>>.Success(products));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        {
            var product = await _productService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById),
                new { id = product.Id },
                ApiResponse<ProductDto>.Success(product, "Producto creado exitosamente."));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
        {
            var product = await _productService.UpdateAsync(id, dto);
            return Ok(ApiResponse<ProductDto>.Success(product, "Producto actualizado exitosamente."));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _productService.DeleteAsync(id);
            return Ok(ApiResponse<string>.Success("", "Producto eliminado exitosamente."));
        }

        /// <summary>
        /// Activa o desactiva un producto (toggle)
        /// </summary>
        [HttpPatch("{id}/toggle-status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var product = await _productService.ToggleStatusAsync(id);
            var message = product.IsActive 
                ? "Producto activado exitosamente." 
                : "Producto desactivado exitosamente.";
            return Ok(ApiResponse<ProductDto>.Success(product, message));
        }
    }
}