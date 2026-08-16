namespace NexusAs.Application.DTOs.Sales
{
    public class CreateSaleDto
    {
        public decimal? DiscountPercent { get; set; }
        public decimal? DiscountAmount { get; set; }
        [Obsolete("Use DiscountAmount or DiscountPercent instead")]
        public decimal Discount { get; set; }
        
        public int PaymentMethodId { get; set; }
        public string? Notes { get; set; }
        public int? CustomerId { get; set; }
        public int? NumberOfInstallments { get; set; }
        public int? PartnerUserId { get; set; }
        
        // BUG 4 FIX: Para prevenir ventas duplicadas
        public string? RequestId { get; set; }
        
        public IEnumerable<CreateSaleDetailDto> Details { get; set; }
            = new List<CreateSaleDetailDto>();
    }
}