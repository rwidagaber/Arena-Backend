using ArenaApplication.Dtos.UserManagement;
using ArenaApplication.Dtos.UserSubscription;
using ArenaApplication.IServices;
using ArenaDomain.Entities.User;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArenaApplication.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserRepository _userRepository;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public UserManagementService(
            IUserRepository userRepository,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _userRepository = userRepository;
            _localizer = localizer;
        }

        public async Task<Result<PagedResult<UserManagementDto>>> GetUsers(
            string? search, 
            bool? isActive, 
            MembershipStatus? membershipStatus, 
            string? subscriptionStatus, 
            int page, 
            int pageSize)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;

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
                        (u.FirstName != null && u.FirstName.ToLower().Contains(cleanSearch)) ||
                        (u.LastName != null && u.LastName.ToLower().Contains(cleanSearch)) ||
                        ((u.FirstName ?? "") + " " + (u.LastName ?? "")).ToLower().Contains(cleanSearch) ||
                        (u.Email != null && u.Email.ToLower().Contains(cleanSearch))
                    );
                }

                if (isActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == isActive.Value);
                }

                if (membershipStatus.HasValue)
                {
                    if (membershipStatus.Value == MembershipStatus.User)
                    {
                        query = query.Where(u => u.MemberProfile == null || !u.MemberProfile.Subscriptions.Any());
                    }
                    else if (membershipStatus.Value == MembershipStatus.ActiveMembership)
                    {
                        query = query.Where(u => u.MemberProfile != null && u.MemberProfile.Subscriptions.Any(s => s.Status == SubscriptionStatus.Active));
                    }
                    else if (membershipStatus.Value == MembershipStatus.ExpiredMembership)
                    {
                        query = query.Where(u => u.MemberProfile != null && u.MemberProfile.Subscriptions.Any() && !u.MemberProfile.Subscriptions.Any(s => s.Status == SubscriptionStatus.Active));
                    }
                }

                if (!string.IsNullOrWhiteSpace(subscriptionStatus))
                {
                    if (subscriptionStatus.Equals("NoSubscription", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(u => u.MemberProfile == null || !u.MemberProfile.Subscriptions.Any());
                    }
                    else if (subscriptionStatus.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(u => u.MemberProfile != null && u.MemberProfile.Subscriptions.Any(s => s.Status == SubscriptionStatus.Active));
                    }
                    else if (subscriptionStatus.Equals("Expired", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(u => u.MemberProfile != null && u.MemberProfile.Subscriptions.Any() && !u.MemberProfile.Subscriptions.Any(s => s.Status == SubscriptionStatus.Active));
                    }
                    else if (subscriptionStatus.Equals("ExpiringSoon", StringComparison.OrdinalIgnoreCase))
                    {
                        var now = DateTime.UtcNow;
                        var sevenDaysFromNow = now.AddDays(7);
                        query = query.Where(u => u.MemberProfile != null &&
                                                 u.MemberProfile.Subscriptions.Any(s => s.Status == SubscriptionStatus.Active && !s.IsDeleted) &&
                                                 u.MemberProfile.Subscriptions
                                                    .Where(s => s.Status == SubscriptionStatus.Active && !s.IsDeleted)
                                                    .Max(s => (DateTime?)s.EndDate) > now &&
                                                 u.MemberProfile.Subscriptions
                                                    .Where(s => s.Status == SubscriptionStatus.Active && !s.IsDeleted)
                                                    .Max(s => (DateTime?)s.EndDate) <= sevenDaysFromNow);
                    }
                }

                int totalCount = await query.CountAsync();

                var users = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

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

                var pagedResult = new PagedResult<UserManagementDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                return Result<PagedResult<UserManagementDto>>.Success(pagedResult);
            }
            catch (Exception)
            {
                return Result<PagedResult<UserManagementDto>>.Failure(_localizer["AnErrorOccurredRetrievingUsers"]);
            }
        }

        public async Task<Result<UserManagementDetailsDto>> GetUserDetails(Guid id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);

                if (user == null || user.IsDeleted)
                {
                    return Result<UserManagementDetailsDto>.Failure(_localizer["UserNotFound"]);
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
                return Result<UserManagementDetailsDto>.Failure(_localizer["AnErrorOccurredRetrievingUserDetails"]);
            }
        }

        public async Task<Result<UserManagementDetailsDto>> GetUserForManage(Guid id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);

                if (user == null || user.IsDeleted)
                {
                    return Result<UserManagementDetailsDto>.Failure(_localizer["UserNotFound"]);
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
                return Result<UserManagementDetailsDto>.Failure(_localizer["AnErrorOccurredLoadingManageUser"]);
            }
        }

        public async Task<Result<bool>> UpdateUserStatus(Guid id, bool isActive)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);

                if (user == null || user.IsDeleted)
                {
                    return Result<bool>.Failure(_localizer["UserNotFound"]);
                }

                user.IsActive = isActive;
                await _userRepository.UpdateAsync(user);

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure(_localizer["AnErrorOccurredSavingUserStatus"]);
            }
        }

        public async Task<Result<bool>> SoftDeleteUser(Guid id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);

                if (user == null || user.IsDeleted)
                {
                    return Result<bool>.Failure(_localizer["UserNotFound"]);
                }

                user.IsDeleted = true;
                user.DeletedAt = DateTime.UtcNow;
                user.IsActive = false;

                await _userRepository.UpdateAsync(user);

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure(_localizer["AnErrorOccurredDeletingUser"]);
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

