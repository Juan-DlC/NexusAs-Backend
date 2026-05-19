namespace NexusAs.Application.DTOs.Partners
{
    public class PartnerSummaryDto
    {
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal TotalDebt { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal PendingDebt { get; set; }
    }
}