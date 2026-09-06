namespace NexusAs.Domain.Entities
{
    public class PartnerConfig : BaseEntity
    {
        public int UserId { get; set; }
        public User? User { get; set; }
        public decimal CommissionPercent { get; set; } = 50;
        public decimal AllianceCommissionPercent { get; set; } = 20;
        public string? Notes { get; set; }
        public ICollection<PartnerProductPrice> ProductPrices { get; set; }
            = new List<PartnerProductPrice>();
        public ICollection<PartnerSale> PartnerSales { get; set; }
            = new List<PartnerSale>();
        public ICollection<PartnerLiquidation> Liquidations { get; set; }
            = new List<PartnerLiquidation>();
        public ICollection<PartnerBusinessCommission> BusinessCommissions { get; set; }
            = new List<PartnerBusinessCommission>();
    }
}