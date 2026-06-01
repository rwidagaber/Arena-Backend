using ArenaDomain.Entities.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaInfrastructure.Repositories
{
    public interface IUserRepository
    {
        Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
