namespace NexusAs.Domain.Entities
{
    public class PartnerProductPrice : BaseEntity
    {
        public int PartnerConfigId { get; set; }
        public PartnerConfig? PartnerConfig { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public decimal CustomCommission { get; set; }
    }
}