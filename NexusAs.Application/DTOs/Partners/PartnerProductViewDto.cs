namespace NexusAs.Application.DTOs.Partners
{
    public class PartnerProductViewDto
    {
        public int ProductId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int Stock { get; set; }
        public decimal PartnerPrice { get; set; }
        public decimal SuggestedPrice { get; set; }
        public bool IsPartnership { get; set; }
    }
}