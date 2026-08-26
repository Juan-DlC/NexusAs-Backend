namespace NexusAs.Application.DTOs.BusinessPartners
{
    public class BusinessPartnerLiquidationPreviewDto
    {
        public int BusinessPartnerId { get; set; }
        public string BusinessPartnerName { get; set; } = string.Empty;
        public decimal CommissionPercent { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<LiquidationSaleDetailDto> Sales { get; set; } = new();
        public decimal TotalGrossProfit { get; set; }
        public decimal TotalPartnerCommission { get; set; }
        public decimal TotalRemainingProfit { get; set; }
        public decimal TotalBusinessPartnerAmount { get; set; }
        public decimal TotalAsAmount { get; set; }
        public int TotalSales { get; set; }
    }
}
