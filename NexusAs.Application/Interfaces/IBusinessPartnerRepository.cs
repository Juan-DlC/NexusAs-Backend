using NexusAs.Domain.Entities;

namespace NexusAs.Application.Interfaces
{
    public interface IBusinessPartnerRepository : IRepository<BusinessPartner>
    {
        Task<(IEnumerable<BusinessPartner> data, int total)> GetAllPagedWithProductsAsync(
            int pageNumber,
            int pageSize,
            string? search,
            bool? isActiveFilter);
        
        Task<BusinessPartner?> GetByIdWithProductsAsync(int id);
        
        Task<List<SaleDetail>> GetSaleDetailsByBusinessPartnerAndDateRangeAsync(
            int businessPartnerId,
            DateTime fromDate,
            DateTime toDate);
        
        Task<List<int>> GetAlreadyLiquidatedSaleIdsAsync(List<int> saleIds);
        
        Task<string?> GetLastLiquidationNumberAsync();
        
        Task<(IEnumerable<BusinessPartnerLiquidation> data, int total)> GetLiquidationsPagedAsync(
            int? businessPartnerId,
            DateTime? fromDate,
            DateTime? toDate,
            int pageNumber,
            int pageSize);
        
        Task<BusinessPartnerLiquidation?> GetLiquidationByIdWithDetailsAsync(int id);
    }
}
