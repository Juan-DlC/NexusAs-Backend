using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusAs.Api.Responses;
using NexusAs.Application.DTOs.Users;
using NexusAs.Application.Interfaces;
using System.Security.Claims;

namespace NexusAs.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
        {
            var users = await _userService.GetAllAsync(includeInactive);
            return Ok(ApiResponse<IEnumerable<UserDto>>.Success(users));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            return Ok(ApiResponse<UserDto>.Success(user!));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            var user = await _userService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById),
                new { id = user.Id },
                ApiResponse<UserDto>.Success(user, "Usuario creado exitosamente."));
        }

        [HttpPatch("{id}/toggle-status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _userService.ToggleStatusAsync(id);
            return Ok(ApiResponse<UserDto>.Success(user,
                $"Usuario {(user.IsActive ? "activado" : "desactivado")} exitosamente."));
        }

        [HttpPatch("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _userService.ChangePasswordAsync(userId, dto);
            return Ok(ApiResponse<string>.Success("", "Contraseña actualizada exitosamente."));
        }
    }
}