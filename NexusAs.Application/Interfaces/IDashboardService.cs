using NexusAs.Application.DTOs.Dashboard;

namespace NexusAs.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetSummaryAsync();
    }
}