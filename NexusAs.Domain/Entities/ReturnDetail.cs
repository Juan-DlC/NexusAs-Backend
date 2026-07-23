namespace NexusAs.Domain.Entities
{
    public class ReturnDetail : BaseEntity
    {
        public int ReturnId { get; set; }
        public Return? Return { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }
}
