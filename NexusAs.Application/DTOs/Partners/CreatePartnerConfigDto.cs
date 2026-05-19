namespace NexusAs.Application.DTOs.Partners
{
    public class CreatePartnerConfigDto
    {
        public int UserId { get; set; }
        public decimal CommissionPercent { get; set; } = 50;
        public string? Notes { get; set; }
    }
}