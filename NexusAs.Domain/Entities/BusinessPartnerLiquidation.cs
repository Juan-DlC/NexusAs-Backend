namespace NexusAs.Domain.Entities
{
    public class BusinessPartnerLiquidation : BaseEntity
    {
        public string LiquidationNumber { get; set; } = string.Empty;
        public int BusinessPartnerId { get; set; }
        public BusinessPartner BusinessPartner { get; set; } = null!;
        public DateTime LiquidationDate { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
        public int ProcessedByUserId { get; set; }
        public User ProcessedByUser { get; set; } = null!;
        
        // Navigation properties
        public ICollection<BusinessPartnerLiquidationDetail> Details { get; set; } = new List<BusinessPartnerLiquidationDetail>();
    }
}
