namespace NexusAs.Application.DTOs.Sales
{
    public class CreateSaleDetailDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}