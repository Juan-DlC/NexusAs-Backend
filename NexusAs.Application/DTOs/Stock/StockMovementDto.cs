namespace NexusAs.Application.DTOs.Stock
{
    public class StockMovementDto
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int StockBefore { get; set; }
        public int StockAfter { get; set; }
        public string? Reason { get; set; }
        public string UserName { get; set; } = string.Empty;
    }
}