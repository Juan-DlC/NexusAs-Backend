namespace NexusAs.Domain.Entities
{
    public class PartnerBusinessCommission : BaseEntity
    {
        public int PartnerConfigId { get; set; }
        public PartnerConfig PartnerConfig { get; set; } = null!;
        
        public int BusinessPartnerId { get; set; }
        public BusinessPartner BusinessPartner { get; set; } = null!;
        
        public decimal CommissionPercent { get; set; }
    }
}
