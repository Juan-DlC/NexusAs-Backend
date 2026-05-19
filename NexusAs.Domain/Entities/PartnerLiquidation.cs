using NexusAs.Domain.Enums;

namespace NexusAs.Domain.Entities
{
    public class PartnerLiquidation : BaseEntity
    {
        public int PartnerConfigId { get; set; }
        public PartnerConfig? PartnerConfig { get; set; }
        public decimal Amount { get; set; }
        public LiquidationType Type { get; set; }
        public DateTime Date { get; set; }
        public string? Notes { get; set; }
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
    }
}