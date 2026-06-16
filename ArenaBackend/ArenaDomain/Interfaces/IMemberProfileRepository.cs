using ArenaDomain.Entities;
using ArenaDomain.Entities.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaInfrastructure.Repositories
{
    public interface IMemberProfileRepository
    {
        Task<MemberProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task UpdateAsync(MemberProfile memberProfile);
    }
}
