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
        
        // TAREA 2: Métodos para dashboard con PaymentMethodName
        Task<IEnumerable<Sale>> GetSalesForDashboardAsync(int userId, string userRole, DateTime from, DateTime to);
        Task<IEnumerable<Sale>> GetRecentSalesAsync(int userId, string userRole, int count);
    }
}