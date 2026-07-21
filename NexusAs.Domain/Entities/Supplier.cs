namespace NexusAs.Domain.Entities
{
    public class Supplier : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
