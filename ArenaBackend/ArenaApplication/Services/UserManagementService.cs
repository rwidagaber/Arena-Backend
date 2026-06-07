using ArenaApplication.Dtos.UserManagement;
using ArenaApplication.IServices;
using ArenaDomain.Entities.User;
using ArenaDomain.Enums;
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
                        .ThenInclude(mp => mp.Subscriptions)
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

                var dtos = users.Select(u => 
                {
                    // Get subscriptions from MemberProfile if it exists
                    var subscriptions = u.MemberProfile?.Subscriptions ?? new List<ArenaDomain.Entities.Subscription.UserSubscription>();

                    return new UserManagementDto
                    {
                        Id = u.Id,
                        FullName = $"{u.FirstName} {u.LastName}".Trim(),
                        Email = u.Email ?? string.Empty,
                        PhoneNumber = u.PhoneNumber ?? string.Empty,
                        RegisterDate = u.MemberProfile?.CreatedAt,
                        IsActive = u.IsActive,
                        // Membership is now determined by subscriptions, not MemberProfile existence
                        IsMember = DetermineMembershipStatus(subscriptions),
                        SubscriptionStatus = DetermineSubscriptionStatus(subscriptions)
                    };
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

                var memberProfile = user.MemberProfile;

                // Get subscriptions - empty list if no MemberProfile or no subscriptions
                var subscriptions = memberProfile?.Subscriptions?.ToList() ?? new List<ArenaDomain.Entities.Subscription.UserSubscription>();

                var activeSubscription = subscriptions.FirstOrDefault(s => s.Status == SubscriptionStatus.Active);

                // Determine membership status based on subscriptions only
                var membershipStatus = DetermineMembershipStatus(subscriptions);
                var subscriptionStatus = DetermineSubscriptionStatus(subscriptions);

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
                    RegisterDate = memberProfile?.CreatedAt,

                    // Membership Info (based on subscriptions)
                    MembershipStatus = membershipStatus,
                    IsMember = membershipStatus != MembershipStatus.User,
                    MemberSince = memberProfile?.CreatedAt,
                    TotalSubscriptions = subscriptions.Count,
                    CurrentSubscriptionStatus = subscriptionStatus,

                    // Current Subscription
                    CurrentSubscription = activeSubscription != null ? MapSubscriptionItem(activeSubscription) : null,

                    // Subscription History (newest first)
                    SubscriptionHistory = subscriptions
                        .OrderByDescending(s => s.CreatedAt)
                        .Select(s => MapSubscriptionItem(s))
                        .ToList()
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

        // --- Private helpers ---

        /// <summary>
        /// Determines membership status based on UserSubscriptions only.
        /// Does NOT rely on MemberProfile existence.
        /// Returns enum value to be localized in views.
        /// </summary>
        private static MembershipStatus DetermineMembershipStatus(IEnumerable<ArenaDomain.Entities.Subscription.UserSubscription>? subscriptions)
        {
            if (subscriptions == null || !subscriptions.Any())
                return MembershipStatus.User;

            if (subscriptions.Any(s => s.Status == SubscriptionStatus.Active))
                return MembershipStatus.ActiveMembership;

            return MembershipStatus.ExpiredMembership;
        }

        /// <summary>
        /// Determines subscription status based on UserSubscriptions only.
        /// Does NOT rely on MemberProfile existence.
        /// Returns enum value to be localized in views.
        /// </summary>
        private static SubscriptionStatus? DetermineSubscriptionStatus(IEnumerable<ArenaDomain.Entities.Subscription.UserSubscription>? subscriptions)
        {
            if (subscriptions == null || !subscriptions.Any())
                return null;

            if (subscriptions.Any(s => s.Status == SubscriptionStatus.Active))
                return SubscriptionStatus.Active;

            return SubscriptionStatus.Expired;
        }

        private static UserSubscriptionItemDto MapSubscriptionItem(ArenaDomain.Entities.Subscription.UserSubscription subscription)
        {
            return new UserSubscriptionItemDto
            {
                PlanName = subscription.Plan?.NameEn ?? string.Empty,
                Status = subscription.Status.ToString(),
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                RemainingSessions = subscription.RemainingSessions,
                DurationDays = (subscription.EndDate - subscription.StartDate).Days,
                CreatedAt = subscription.CreatedAt
            };
        }
    }
}

