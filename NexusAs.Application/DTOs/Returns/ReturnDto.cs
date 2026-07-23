namespace NexusAs.Application.DTOs.Returns
{
    public class ReturnDto
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? Notes { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public List<ReturnDetailDto> Details { get; set; } = new();
    }

    public class ReturnDetailDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }
}
