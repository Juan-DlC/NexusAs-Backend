using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusAs.Api.Responses;
using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Suppliers;
using NexusAs.Application.Interfaces;

namespace NexusAs.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SupplierController : ControllerBase
    {
        private readonly ISupplierService _supplierService;

        public SupplierController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        /// <summary>
        /// Obtiene lista paginada de proveedores
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var suppliers = await _supplierService.GetAllAsync(pageNumber, pageSize, search);
            return Ok(ApiResponse<PagedResponseDto<SupplierDto>>.Success(suppliers));
        }

        /// <summary>
        /// Obtiene un proveedor por ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var supplier = await _supplierService.GetByIdAsync(id);
            return Ok(ApiResponse<SupplierDto>.Success(supplier!));
        }

        /// <summary>
        /// Crea un nuevo proveedor
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateSupplierDto dto)
        {
            var supplier = await _supplierService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById),
                new { id = supplier.Id },
                ApiResponse<SupplierDto>.Success(supplier, "Proveedor creado exitosamente."));
        }

        /// <summary>
        /// Actualiza un proveedor existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSupplierDto dto)
        {
            var supplier = await _supplierService.UpdateAsync(id, dto);
            return Ok(ApiResponse<SupplierDto>.Success(supplier, "Proveedor actualizado exitosamente."));
        }

        /// <summary>
        /// Desactiva un proveedor (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _supplierService.DeleteAsync(id);
            return Ok(ApiResponse<string>.Success("", "Proveedor desactivado exitosamente."));
        }
    }
}
