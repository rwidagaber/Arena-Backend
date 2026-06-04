using ArenaDomain.Entities.User;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ArenaDomain.Interfaces
{
    public interface IUserRepository
    {
        IQueryable<ApplicationUser> GetAll();
        Task<ApplicationUser?> GetByIdAsync(Guid id);
        Task UpdateAsync(ApplicationUser user);
    }
}
