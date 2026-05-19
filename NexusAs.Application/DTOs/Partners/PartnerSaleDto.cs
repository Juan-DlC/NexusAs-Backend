namespace NexusAs.Application.DTOs.Partners
{
    public class PartnerSaleDto
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal CostPrice { get; set; }
        public decimal PartnerPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal CommissionPercent { get; set; }
        public decimal PartnerEarning { get; set; }
        public decimal AsEarning { get; set; }
        public DateTime Date { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
    }
}