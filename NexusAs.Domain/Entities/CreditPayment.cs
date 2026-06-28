namespace NexusAs.Domain.Entities
{
    public class CreditPayment : BaseEntity
    {
        public int CreditId { get; set; }
        public Credit? Credit { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string? Notes { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
    }
}