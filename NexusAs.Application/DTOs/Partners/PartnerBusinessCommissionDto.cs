namespace NexusAs.Application.DTOs.Partners
{
    public class PartnerBusinessCommissionDto
    {
        public int BusinessPartnerId { get; set; }
        public string BusinessPartnerName { get; set; } = string.Empty;
        public decimal CommissionPercent { get; set; }
    }
}
