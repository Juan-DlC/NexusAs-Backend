namespace NexusAs.Application.DTOs.Sales
{
    public class CreateSaleDto
    {
        // BUG 4 FIX: Agregar descuento por porcentaje
        public decimal? DiscountPercent { get; set; }  // porcentaje 0-100
        public decimal? DiscountAmount { get; set; }   // monto fijo (mantener compatibilidad)
        [Obsolete("Use DiscountAmount or DiscountPercent instead")]
        public decimal Discount { get; set; }
        
        public int PaymentMethodId { get; set; }
        public string? Notes { get; set; }
        public int? CustomerId { get; set; }
        public int? NumberOfInstallments { get; set; }
        public int? PartnerUserId { get; set; }
        public IEnumerable<CreateSaleDetailDto> Details { get; set; }
            = new List<CreateSaleDetailDto>();
    }
}