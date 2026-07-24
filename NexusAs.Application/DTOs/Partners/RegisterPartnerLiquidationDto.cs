namespace NexusAs.Application.DTOs.Partners
{
    public class RegisterPartnerLiquidationDto
    {
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
        public int? SaleId { get; set; }
    }
}