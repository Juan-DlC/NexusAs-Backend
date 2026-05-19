using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusAs.Api.Responses;
using NexusAs.Application.DTOs.Dashboard;
using NexusAs.Application.Interfaces;

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
    }
}