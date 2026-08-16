namespace NexusAs.Application.DTOs.Dashboard
{
    public class DashboardSummaryDto
    {
        public int TotalSalesToday { get; set; }
        public decimal TotalRevenueToday { get; set; }
        public int LowStockCount { get; set; }
        public int PendingCreditsCount { get; set; }
        public decimal PendingCreditsAmount { get; set; }
        public IEnumerable<RecentSaleDto> RecentSales { get; set; } = new List<RecentSaleDto>();
    }
}
