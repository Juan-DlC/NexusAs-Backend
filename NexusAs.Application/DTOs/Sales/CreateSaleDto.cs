namespace NexusAs.Application.DTOs.Sales
{
    public class CreateSaleDto
    {
        public decimal Discount { get; set; }
        public int PaymentMethodId { get; set; }
        public string? Notes { get; set; }
        public int? CustomerId { get; set; }
        public int? NumberOfInstallments { get; set; }
        public IEnumerable<CreateSaleDetailDto> Details { get; set; }
            = new List<CreateSaleDetailDto>();
    }
}