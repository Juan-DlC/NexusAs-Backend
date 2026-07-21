namespace NexusAs.Domain.Entities
{
    public class PaymentMethodEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}
