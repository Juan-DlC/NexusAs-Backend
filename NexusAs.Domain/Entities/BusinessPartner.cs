namespace NexusAs.Domain.Entities
{
    public class BusinessPartner : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public decimal CommissionPercent { get; set; }
        
        // Navigation properties
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<BusinessPartnerLiquidation> Liquidations { get; set; } = new List<BusinessPartnerLiquidation>();
        public ICollection<PartnerBusinessCommission> PartnerCommissions { get; set; } = new List<PartnerBusinessCommission>();
    }
}
