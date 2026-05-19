namespace NexusAs.Application.DTOs.Customers
{
    public class CreateCustomerDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Document { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
    }
}