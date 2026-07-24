namespace NexusAs.Application.DTOs.Partners
{
    public class PartnerAdminSummaryDto
    {
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public decimal TotalDebt { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal PendingDebt { get; set; }
        public int InvoiceCount { get; set; }
        public decimal AllianceCommissionPercent { get; set; }
        public decimal CommissionPercent { get; set; }
    }
}
