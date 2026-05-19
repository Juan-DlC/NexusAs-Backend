namespace NexusAs.Application.DTOs.Partners
{
    public class PartnerProductPriceDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal CustomCommission { get; set; }
    }
}