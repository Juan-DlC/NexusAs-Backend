using NexusAs.Domain.Entities;

namespace NexusAs.Application.Interfaces
{
    public interface ICreditRepository : IRepository<Credit>
    {
        Task<IEnumerable<Credit>> GetAllWithDetailsAsync(string? status = null);
        Task<Credit?> GetByIdWithDetailsAsync(int id);
    }
}