namespace NexusAs.Application.DTOs.Sales
{
    public class CreditInfoDto
    {
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public int NumberOfInstallments { get; set; }
        public List<InstallmentInfoDto> Installments { get; set; } = new();
    }

    public class InstallmentInfoDto
    {
        public int Number { get; set; }
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
    }
}