using NexusAs.Application.DTOs.Products;

namespace NexusAs.Application.DTOs.Dashboard
{
    public class DashboardDto
    {
        public decimal TodaySales { get; set; }
        public decimal TodayCash { get; set; }
        public decimal TodayCredit { get; set; }
        public int TodayTransactions { get; set; }
        public int PendingCredits { get; set; }
        public decimal PendingCreditsAmount { get; set; }
        public IEnumerable<ProductDto> LowStockProducts { get; set; }
            = new List<ProductDto>();
    }
}