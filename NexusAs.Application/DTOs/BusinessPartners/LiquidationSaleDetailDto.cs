namespace NexusAs.Application.DTOs.BusinessPartners
{
    public class LiquidationSaleDetailDto
    {
        public int SaleId { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string PaymentMethodName { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal PartnerCommissionPercent { get; set; }
        public decimal PartnerCommissionAmount { get; set; }
        public decimal RemainingProfit { get; set; }
        public decimal BusinessPartnerCommissionPercent { get; set; }
        public decimal BusinessPartnerAmount { get; set; }
        public decimal AsAmount { get; set; }
    }
}
