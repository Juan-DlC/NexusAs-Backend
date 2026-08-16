using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusAs.Api.Responses;
using NexusAs.Application.DTOs.Dashboard;
using NexusAs.Application.Interfaces;
using System.Security.Claims;

namespace NexusAs.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSummary()
        {
            var dashboard = await _dashboardService.GetSummaryAsync();
            return Ok(ApiResponse<DashboardDto>.Success(dashboard));
        }

        // TAREA 2: Nuevo endpoint para dashboard con ventas recientes
        [HttpGet("summary")]
        [Authorize(Roles = "Admin,Seller,Partner")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Seller";

            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(ApiResponse<object>.Fail("Usuario no autenticado"));

            var summary = await _dashboardService.GetDashboardSummaryAsync(userId, userRole);
            return Ok(ApiResponse<DashboardSummaryDto>.Success(summary));
        }
    }
}
