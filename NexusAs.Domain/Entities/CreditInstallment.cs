namespace NexusAs.Domain.Entities
{
    public class CreditInstallment : BaseEntity
    {
        public int CreditId { get; set; }
        public Credit? Credit { get; set; }
        public int Number { get; set; }
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; } = false;
    }
}