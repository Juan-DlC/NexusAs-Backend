namespace NexusAs.Domain.Entities
{
    public class PartnerSale : BaseEntity
    {
        public int PartnerConfigId { get; set; }
        public PartnerConfig? PartnerConfig { get; set; }
        public int SaleId { get; set; }
        public Sale? Sale { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int Quantity { get; set; }
        public decimal CostPrice { get; set; }
        public decimal PartnerPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal CommissionPercent { get; set; }
        public decimal PartnerEarning { get; set; }
        public decimal AsEarning { get; set; }
        public DateTime Date { get; set; }
    }
}