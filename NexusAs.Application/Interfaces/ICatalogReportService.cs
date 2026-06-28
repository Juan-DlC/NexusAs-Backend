namespace NexusAs.Application.Interfaces
{
    public interface ICatalogReportService
    {
        Task<byte[]> GenerateCatalogPdfAsync(int? categoryId = null);
    }
}
