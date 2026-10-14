using NexusAs.Domain.Entities;
using NexusAs.Domain.Enums;

namespace NexusAs.Application.Interfaces
{
    public interface ICreditRepository : IRepository<Credit>
    {
        Task<IEnumerable<Credit>> GetAllWithDetailsAsync(string? status = null);
        Task<Credit?> GetByIdWithDetailsAsync(int id);
        Task<Credit?> GetByIdFullDetailsAsync(int id);
        Task<(IEnumerable<Credit> Items, int TotalRecords)> GetAllPagedAsync(
            int pageNumber, int pageSize,
            string? search = null,
            string? status = null,
            int userId = 0,
            string userRole = "Admin");
    }
}