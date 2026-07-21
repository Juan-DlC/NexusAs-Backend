namespace NexusAs.Application.DTOs.Suppliers
{
    public class CreateSupplierDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
    }
}
