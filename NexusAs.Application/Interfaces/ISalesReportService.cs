namespace NexusAs.Application.Interfaces
{
    public interface ISalesReportService
    {
        Task<byte[]> GenerateSalesReportPdfAsync(DateTime from, DateTime to);
        Task<byte[]> GenerateSalesReportExcelAsync(DateTime from, DateTime to);
    }
}
