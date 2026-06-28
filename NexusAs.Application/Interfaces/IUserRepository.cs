using NexusAs.Domain.Entities;

namespace NexusAs.Application.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByIdIncludingInactiveAsync(int id);
    }
}