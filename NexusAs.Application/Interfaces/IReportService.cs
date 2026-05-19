namespace NexusAs.Application.Interfaces
{
    public interface IReportService
    {
        Task<byte[]> GenerateSalesReportPdfAsync(DateTime from, DateTime to);
        Task<byte[]> GenerateSalesReportExcelAsync(DateTime from, DateTime to);
        Task<byte[]> GenerateCatalogPdfAsync(int? categoryId = null);
        Task<byte[]> GenerateSaleReceiptAsync(int saleId);
        Task<byte[]> GeneratePartnerStatementPdfAsync(int partnerConfigId, DateTime from, DateTime to);
    }
}