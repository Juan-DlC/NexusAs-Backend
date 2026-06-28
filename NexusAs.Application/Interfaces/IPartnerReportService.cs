using NexusAs.Application.DTOs.Partners;

namespace NexusAs.Application.Interfaces
{
    public interface IPartnerReportService
    {
        Task<byte[]> GeneratePartnerStatementPdfAsync(int partnerConfigId, DateTime from, DateTime to);
        Task<AllianceReportDto> GetAllianceReportAsync(DateTime from, DateTime to);
    }
}
