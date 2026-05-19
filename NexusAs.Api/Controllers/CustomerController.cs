using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusAs.Api.Responses;
using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Customers;
using NexusAs.Application.Interfaces;

namespace NexusAs.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var customers = await _customerService.GetAllAsync(pageNumber, pageSize, search);
            return Ok(ApiResponse<PagedResponseDto<CustomerDto>>.Success(customers));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var customer = await _customerService.GetByIdAsync(id);
            return Ok(ApiResponse<CustomerDto>.Success(customer!));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
        {
            var customer = await _customerService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById),
                new { id = customer.Id },
                ApiResponse<CustomerDto>.Success(customer, "Cliente creado exitosamente."));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto dto)
        {
            var customer = await _customerService.UpdateAsync(id, dto);
            return Ok(ApiResponse<CustomerDto>.Success(customer, "Cliente actualizado exitosamente."));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _customerService.DeleteAsync(id);
            return Ok(ApiResponse<string>.Success("", "Cliente eliminado exitosamente."));
        }
    }
}