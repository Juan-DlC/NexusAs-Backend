namespace NexusAs.Application.DTOs.BusinessPartners
{
    public class CreateBusinessPartnerDto
    {
        public string Name { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public decimal CommissionPercent { get; set; }
    }
}
