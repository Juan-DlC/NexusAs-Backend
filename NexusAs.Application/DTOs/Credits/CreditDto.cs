namespace NexusAs.Application.DTOs.Credits
{
    public class CreditDto
    {
        public int Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Notes { get; set; }
        // BUG 3 FIX: Agregar SellerName para mostrar nombre de la socia
        public string? SellerName { get; set; }
    }
}