namespace NexusAs.Domain.Entities
{
    public class BusinessPartnerLiquidationDetail : BaseEntity
    {
        public int LiquidationId { get; set; }
        public BusinessPartnerLiquidation Liquidation { get; set; } = null!;
        public int SaleId { get; set; }
        public Sale Sale { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal PartnerCommissionPercent { get; set; }
        public decimal BusinessPartnerCommissionPercent { get; set; }
        public decimal PartnerCommissionAmount { get; set; }
        public decimal RemainingProfit { get; set; }
        public decimal BusinessPartnerAmount { get; set; }
        public decimal AsAmount { get; set; }
    }
}
