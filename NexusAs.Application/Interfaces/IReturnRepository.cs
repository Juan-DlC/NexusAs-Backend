using NexusAs.Domain.Entities;

namespace NexusAs.Application.Interfaces
{
    public interface IReturnRepository : IRepository<Return>
    {
        Task<IEnumerable<Return>> GetAllWithDetailsAsync(DateTime? from = null, DateTime? to = null);
    }
}
