using NexusAs.Application.DTOs.Auth;

namespace NexusAs.Application.Interfaces
{
    public interface IAuthService
    {
        Task<TokenDto> LoginAsync(LoginDto dto);
        Task<TokenDto> CreateUserAsync(string username, string password, string fullName, string role);
    }
}