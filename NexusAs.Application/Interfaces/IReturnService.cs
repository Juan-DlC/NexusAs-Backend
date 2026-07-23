using NexusAs.Application.DTOs.Common;
using NexusAs.Application.DTOs.Returns;

namespace NexusAs.Application.Interfaces
{
    public interface IReturnService
    {
        Task<PagedResponseDto<ReturnDto>> GetAllAsync(
            int pageNumber, int pageSize, DateTime? from = null, DateTime? to = null);
        Task<ReturnDto?> GetByIdAsync(int id);
        Task<ReturnDto> CreateAsync(CreateReturnDto dto, int userId);
    }
}
