using NexusAs.Application.DTOs.Sales;

namespace NexusAs.Application.DTOs.Sales
{
    public class SaleDto
    {
        public int Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? CustomerName { get; set; }
        public int? CustomerId { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public CreditInfoDto? CreditInfo { get; set; }
        public IEnumerable<SaleDetailDto> Details { get; set; } = new List<SaleDetailDto>();
    }
}