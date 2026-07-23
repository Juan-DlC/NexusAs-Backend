using NexusAs.Application.DTOs.Partners;


namespace NexusAs.Application.Interfaces
{
    public interface IPartnerService
    {
        Task<IEnumerable<PartnerConfigDto>> GetAllPartnersAsync();
        Task<PartnerConfigDto?> GetPartnerByIdAsync(int partnerConfigId);
        Task<PartnerConfigDto> CreatePartnerConfigAsync(CreatePartnerConfigDto dto);
        Task<PartnerConfigDto> UpdateCommissionAsync(int partnerConfigId, UpdatePartnerCommissionDto dto);
        Task<PartnerProductPriceDto> SetProductPriceAsync(int partnerConfigId, SetPartnerProductPriceDto dto);
        Task<IEnumerable<PartnerProductPriceDto>> GetProductPricesAsync(int partnerConfigId);
        Task<PartnerSummaryDto> GetPartnerSummaryAsync(int partnerConfigId);
        Task<IEnumerable<PartnerSaleDto>> GetPartnerSalesAsync(int partnerConfigId, DateTime? from = null, DateTime? to = null);
        Task<PartnerLiquidationDto> RegisterLiquidationAsync(int partnerConfigId, RegisterPartnerLiquidationDto dto);
        Task<IEnumerable<PartnerLiquidationDto>> GetLiquidationsAsync(int partnerConfigId);
        Task<AllianceReportDto> GetAllianceReportAsync(DateTime from, DateTime to);
        Task<byte[]> GeneratePartnerStatementAsync(int partnerConfigId, DateTime from, DateTime to);
        Task<IEnumerable<PartnerProductViewDto>> GetMyProductsAsync(int partnerConfigId);
    }
}