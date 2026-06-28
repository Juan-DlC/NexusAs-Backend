using NexusAs.Application.DTOs.Users;

namespace NexusAs.Application.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllAsync(bool includeInactive = false);
        Task<UserDto?> GetByIdAsync(int id);
        Task<UserDto> CreateAsync(CreateUserDto dto);
        Task<UserDto> ToggleStatusAsync(int id);
        Task ChangePasswordAsync(int userId, ChangePasswordDto dto);
    }
}