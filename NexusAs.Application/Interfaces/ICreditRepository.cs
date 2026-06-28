using NexusAs.Domain.Entities;

namespace NexusAs.Application.Interfaces
{
    public interface ICreditRepository : IRepository<Credit>
    {
        Task<IEnumerable<Credit>> GetAllWithDetailsAsync(string? status = null);
        Task<Credit?> GetByIdWithDetailsAsync(int id);
        Task<Credit?> GetByIdFullDetailsAsync(int id);
        Task<(IEnumerable<Credit> Items, int TotalRecords)> GetAllPagedAsync(
          string? status, string? search, int pageNumber, int pageSize);  
    }
}