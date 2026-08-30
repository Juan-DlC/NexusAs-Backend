using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusAs.Application.DTOs.Auth;
using NexusAs.Application.Interfaces;
using NexusAs.Api.Responses;

namespace NexusAs.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var token = await _authService.LoginAsync(dto);
            return Ok(ApiResponse<TokenDto>.Success(token, "Login exitoso."));
        }

        // SEGURIDAD: Solo administradores pueden crear nuevos usuarios
        [Authorize(Roles = "Admin")]
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromQuery] string username,
            [FromQuery] string password,
            [FromQuery] string fullName,
            [FromQuery] string role = "Seller")
        {
            var token = await _authService.CreateUserAsync(
                username, password, fullName, role);
            return Ok(ApiResponse<TokenDto>.Success(token, "Usuario creado exitosamente."));
        }
    }
}