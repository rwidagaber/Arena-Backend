using ArenaApplication.Dtos.UserManagement;
using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArenaApplication.IServices
{
    public interface IUserManagementService
    {
        Task<Result<List<UserManagementDto>>> GetUsers(string search);
        Task<Result<UserManagementDetailsDto>> GetUserDetails(Guid id);
        Task<Result<UserManagementDetailsDto>> GetUserForManage(Guid id);
        Task<Result<bool>> UpdateUserStatus(Guid id, bool isActive);
        Task<Result<bool>> SoftDeleteUser(Guid id);
    }
}
