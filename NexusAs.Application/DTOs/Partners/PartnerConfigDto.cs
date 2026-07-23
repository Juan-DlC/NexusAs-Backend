namespace NexusAs.Application.DTOs.Partners
{
    public class PartnerConfigDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public decimal CommissionPercent { get; set; }
        public decimal AllianceCommissionPercent { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
    }
}