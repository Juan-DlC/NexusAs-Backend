namespace NexusAs.Application.DTOs.Stock
{
    public class StockEntryDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string? Reason { get; set; }
    }
}