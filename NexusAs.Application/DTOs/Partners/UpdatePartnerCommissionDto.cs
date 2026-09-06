namespace NexusAs.Application.DTOs.Partners
{
    public class UpdatePartnerCommissionDto
    {
        public decimal CommissionPercent { get; set; }
        public decimal AllianceCommissionPercent { get; set; }
        public List<PartnerBusinessCommissionDto> BusinessCommissions { get; set; } = new();
    }
}
