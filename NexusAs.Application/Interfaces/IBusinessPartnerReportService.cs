namespace NexusAs.Application.Interfaces
{
    public interface IBusinessPartnerReportService
    {
        Task<byte[]> GenerateLiquidationPdfAsync(int liquidationId);
    }
}
