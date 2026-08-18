namespace NexusAs.Application.DTOs.Dashboard
{
    public class DashboardSummaryDto
    {
        // Campos con nombres que el frontend espera
        public int TodaySalesCount { get; set; }
        public decimal TodaySalesAmount { get; set; }
        public int LowStockCount { get; set; }
        public int PendingCreditsCount { get; set; }
        public decimal PendingCreditsAmount { get; set; }
        public IEnumerable<RecentSaleDto> RecentSales { get; set; } = new List<RecentSaleDto>();
        
        // Campos legacy para compatibilidad (deprecated)
        [System.Obsolete("Use TodaySalesCount instead")]
        public int TotalSalesToday => TodaySalesCount;
        
        [System.Obsolete("Use TodaySalesAmount instead")]
        public decimal TotalRevenueToday => TodaySalesAmount;
    }
}
