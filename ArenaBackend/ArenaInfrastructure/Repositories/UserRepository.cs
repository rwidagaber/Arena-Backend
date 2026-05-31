using ArenaDomain.Entities.User;
using ArenaInfrastructure.Repositories;
using System.Security.Principal;

namespace ArenaAPI
{
    public class UserRepository : IUserRepository
    {
        public Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}