using ArenaDomain.Entities.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.IServices.User
{
    public interface IUserQueryService
    {
        Task<ApplicationUser?> GetByIdAsync(Guid userId);

    }
}
