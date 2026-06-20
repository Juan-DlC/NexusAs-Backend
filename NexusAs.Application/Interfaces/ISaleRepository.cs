using NexusAs.Domain.Entities;

namespace NexusAs.Application.Interfaces
{
    public interface ISaleRepository : IRepository<Sale>
    {
        Task<IEnumerable<Sale>> GetSalesWithDetailsAsync(
            int userId, string userRole, DateTime? from, DateTime? to);
        Task<Sale?> GetSaleByIdWithDetailsAsync(int id);
    }
}