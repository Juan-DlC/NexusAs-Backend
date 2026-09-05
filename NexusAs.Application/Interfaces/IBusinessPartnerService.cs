using NexusAs.Application.DTOs.BusinessPartners;
using NexusAs.Application.DTOs.Common;

namespace NexusAs.Application.Interfaces
{
    public interface IBusinessPartnerService
    {
        Task<PagedResponseDto<BusinessPartnerDto>> GetAllAsync(
            int pageNumber = 1, 
            int pageSize = 10, 
            string? search = null, 
            bool? isActiveFilter = null);
        
        Task<BusinessPartnerDto?> GetByIdAsync(int id);
        Task<BusinessPartnerDto> CreateAsync(CreateBusinessPartnerDto dto);
        Task<BusinessPartnerDto> UpdateAsync(int id, UpdateBusinessPartnerDto dto);
        Task<BusinessPartnerDto> UpdateCommissionAsync(int id, UpdateCommissionDto dto);
        Task DeleteAsync(int id);
        Task<BusinessPartnerDto> ToggleStatusAsync(int id);
        
        Task<BusinessPartnerLiquidationPreviewDto> PreviewLiquidationAsync(
            int businessPartnerId, 
            DateTime fromDate, 
            DateTime toDate);
        
        Task<BusinessPartnerLiquidationDto> ConfirmLiquidationAsync(
            int businessPartnerId, 
            DateTime fromDate, 
            DateTime toDate, 
            int processedByUserId, 
            string? notes = null);
        
        Task<PagedResponseDto<BusinessPartnerLiquidationDto>> GetLiquidationsAsync(
            int? businessPartnerId = null, 
            DateTime? fromDate = null, 
            DateTime? toDate = null, 
            int pageNumber = 1, 
            int pageSize = 10);
        
        Task<BusinessPartnerLiquidationDto?> GetLiquidationByIdAsync(int id);
    }
}
