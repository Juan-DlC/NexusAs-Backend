using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Customers;

namespace NexusAs.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<PagedResponseDto<CustomerDto>> GetAllAsync(
            int pageNumber, int pageSize, string? search = null);
        Task<CustomerDto?> GetByIdAsync(int id);
        Task<CustomerDto> CreateAsync(CreateCustomerDto dto);
        Task<CustomerDto> UpdateAsync(int id, UpdateCustomerDto dto);
        Task DeleteAsync(int id);
    }
}