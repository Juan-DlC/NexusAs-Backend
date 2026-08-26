namespace NexusAs.Application.DTOs.BusinessPartners
{
    public class BusinessPartnerLiquidationDto
    {
        public int Id { get; set; }
        public string LiquidationNumber { get; set; } = string.Empty;
        public int BusinessPartnerId { get; set; }
        public string BusinessPartnerName { get; set; } = string.Empty;
        public DateTime LiquidationDate { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
        public int ProcessedByUserId { get; set; }
        public string ProcessedByUserName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalSales { get; set; }
        public List<LiquidationSaleDetailDto> Details { get; set; } = new();
    }
}
