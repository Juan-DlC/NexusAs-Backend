namespace NexusAs.Application.DTOs.Partners
{
    public class PartnerInvoiceDto
    {
        public int SaleId { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethodName { get; set; } = string.Empty;
        public string CreditStatus { get; set; } = string.Empty; // "NoCredit","Pending","Partial","Paid"
        public decimal PendingAmount { get; set; }
    }
}
