using NexusAs.Domain.Enums;

namespace NexusAs.Domain.Entities
{
    public class Return : BaseEntity
    {
        public int SaleId { get; set; }
        public Sale? Sale { get; set; }
        public DateTime Date { get; set; }
        public string? Notes { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public ReturnType Type { get; set; }
        public decimal TotalAmount { get; set; }
        public ICollection<ReturnDetail> ReturnDetails { get; set; } = new List<ReturnDetail>();
    }
}
