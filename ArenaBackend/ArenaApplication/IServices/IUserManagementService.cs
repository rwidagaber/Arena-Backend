using ArenaApplication.Dtos.UserManagement;
using ArenaApplication.Dtos.UserSubscription;
using ArenaDomain.Enums;
using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArenaApplication.IServices
{
    public interface IUserManagementService
    {
        Task<Result<PagedResult<UserManagementDto>>> GetUsers(string? search, bool? isActive, MembershipStatus? membershipStatus, string? subscriptionStatus, int page, int pageSize, string? sortBy = "RegisterDate", bool isAscending = false);
        Task<Result<UserManagementDetailsDto>> GetUserDetails(Guid id);
        Task<Result<UserManagementDetailsDto>> GetUserForManage(Guid id);
        Task<Result<bool>> UpdateUserStatus(Guid id, bool isActive);
        Task<Result<bool>> AddManualSubscription(Guid userId, Guid planId, string adminName);
        Task<Result<bool>> CancelActiveSubscription(Guid userId, Guid subscriptionId, string adminName);
        Task<Result<bool>> SoftDeleteUser(Guid id);
    }
}
