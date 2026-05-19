namespace NexusAs.Application.DTOs.Stock
{
    public class StockAdjustmentDto
    {
        public int ProductId { get; set; }
        public int NewStock { get; set; }
        public string? Reason { get; set; }
    }
}