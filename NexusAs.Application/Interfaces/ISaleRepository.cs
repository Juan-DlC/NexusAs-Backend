using NexusAs.Domain.Entities;

namespace NexusAs.Application.Interfaces
{
    public interface ISaleRepository : IRepository<Sale>
    {
        Task<(IEnumerable<Sale> Items, int TotalRecords)> GetSalesWithDetailsAsync(
            int userId, string userRole, DateTime? from, DateTime? to,
            string? search, int pageNumber, int pageSize);
        Task<Sale?> GetSaleByIdWithDetailsAsync(int id);
        Task<IEnumerable<Sale>> GetTodaySalesAsync(DateTime from, DateTime to);
    }
}