namespace NexusAs.Application.DTOs.Partners
{
    public class AllianceReportDto
    {
        public List<AllianceStockItemDto> Stock { get; set; } = new();
        public List<AllianceSoldItemDto> Sold { get; set; } = new();
        public decimal TotalSoldAmount { get; set; }
        public int TotalSoldUnits { get; set; }
    }

    public class AllianceStockItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
    }

    public class AllianceSoldItemDto
    {
        public DateTime Date { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public bool SoldByPartner { get; set; }
    }
}