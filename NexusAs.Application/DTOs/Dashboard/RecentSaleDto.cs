namespace NexusAs.Application.DTOs.Dashboard
{
    public class RecentSaleDto
    {
        public string SaleNumber { get; set; } = string.Empty;
        public string CustomerOrSellerName { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string PaymentMethodName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}
