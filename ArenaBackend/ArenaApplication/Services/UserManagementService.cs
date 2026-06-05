using ArenaApplication.Dtos.UserManagement;
using ArenaApplication.IServices;
using ArenaDomain.Entities.User;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArenaApplication.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserRepository _userRepository;

        public UserManagementService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<List<UserManagementDto>>> GetUsers(string search)
        {
            try
            {
                var query = _userRepository.GetAll()
                    .Include(u => u.MemberProfile)
                    .AsNoTracking();

                // Exclude soft-deleted users
                query = query.Where(u => !u.IsDeleted);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var cleanSearch = search.Trim().ToLower();
                    query = query.Where(u =>
                        (u.FirstName + " " + u.LastName).ToLower().Contains(cleanSearch) ||
                        (u.Email != null && u.Email.ToLower().Contains(cleanSearch))
                    );
                }

                var users = await query.ToListAsync();

                var dtos = users.Select(u => new UserManagementDto
                {
                    Id = u.Id,
                    FullName = $"{u.FirstName} {u.LastName}".Trim(),
                    Email = u.Email ?? string.Empty,
                    PhoneNumber = u.PhoneNumber ?? string.Empty,
                    RegisterDate = u.MemberProfile?.CreatedAt,
                    IsActive = u.IsActive
                }).ToList();

                return Result<List<UserManagementDto>>.Success(dtos);
            }
            catch (Exception)
            {
                return Result<List<UserManagementDto>>.Failure("An error occurred while retrieving users.");
            }
        }

        public async Task<Result<UserManagementDetailsDto>> GetUserDetails(Guid id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);

                if (user == null || user.IsDeleted)
                {
                    return Result<UserManagementDetailsDto>.Failure("User not found.");
                }

                var dto = new UserManagementDetailsDto
                {
                    Id = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    Username = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    PreferredLanguage = user.PreferredLanguage ?? string.Empty,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    RegisterDate = user.MemberProfile?.CreatedAt
                };

                return Result<UserManagementDetailsDto>.Success(dto);
            }
            catch (Exception)
            {
                return Result<UserManagementDetailsDto>.Failure("An error occurred while retrieving user details.");
            }
        }

        public async Task<Result<UserManagementDetailsDto>> GetUserForManage(Guid id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);

                if (user == null || user.IsDeleted)
                {
                    return Result<UserManagementDetailsDto>.Failure("User not found.");
                }

                var dto = new UserManagementDetailsDto
                {
                    Id = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    Email = user.Email ?? string.Empty,
                    IsActive = user.IsActive
                };

                return Result<UserManagementDetailsDto>.Success(dto);
            }
            catch (Exception)
            {
                return Result<UserManagementDetailsDto>.Failure("An error occurred while loading manage user details.");
            }
        }

        public async Task<Result<bool>> UpdateUserStatus(Guid id, bool isActive)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);

                if (user == null || user.IsDeleted)
                {
                    return Result<bool>.Failure("User not found.");
                }

                user.IsActive = isActive;
                await _userRepository.UpdateAsync(user);

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure("An error occurred while updating user status.");
            }
        }

        public async Task<Result<bool>> SoftDeleteUser(Guid id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);

                if (user == null || user.IsDeleted)
                {
                    return Result<bool>.Failure("User not found.");
                }

                user.IsDeleted = true;
                user.DeletedAt = DateTime.UtcNow;
                user.IsActive = false;

                await _userRepository.UpdateAsync(user);

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure("An error occurred while deleting the user.");
            }
        }
    }
}
