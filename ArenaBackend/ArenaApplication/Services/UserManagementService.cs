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

using ArenaDomain.Entities.Subscription;
using ArenaDomain.Entities.Payments;
using ArenaDomain.Entities;

namespace ArenaApplication.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserRepository _userRepository;
        private readonly IGenericRepository<ArenaDomain.Entities.Subscription.SubscriptionPlan, Guid> _planRepository;
        private readonly IGenericRepository<ArenaDomain.Entities.Subscription.UserSubscription, Guid> _subscriptionRepository;
        private readonly IGenericRepository<MemberProfile, Guid> _memberProfileRepository;
        private readonly IGenericRepository<ArenaDomain.Entities.Payments.Payment, Guid> _paymentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public UserManagementService(
            IUserRepository userRepository,
            IGenericRepository<ArenaDomain.Entities.Subscription.SubscriptionPlan, Guid> planRepository,
            IGenericRepository<ArenaDomain.Entities.Subscription.UserSubscription, Guid> subscriptionRepository,
            IGenericRepository<MemberProfile, Guid> memberProfileRepository,
            IGenericRepository<ArenaDomain.Entities.Payments.Payment, Guid> paymentRepository,
            IUnitOfWork unitOfWork,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _userRepository = userRepository;
            _planRepository = planRepository;
            _subscriptionRepository = subscriptionRepository;
            _memberProfileRepository = memberProfileRepository;
            _paymentRepository = paymentRepository;
            _unitOfWork = unitOfWork;
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
                            .ThenInclude(s => s.Payments)
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
                }

                query = query.OrderByDescending(u => u.CreatedAt);

                int totalCount = await query.CountAsync();

                var users = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var dtos = users.Select(u => 
                {
                    // Get subscriptions from MemberProfile if it exists
                    var subscriptions = u.MemberProfile?.Subscriptions ?? new List<ArenaDomain.Entities.Subscription.UserSubscription>();
                    var activeSubscription = subscriptions.FirstOrDefault(s => s.Status == SubscriptionStatus.Active);
                    var isManualActive = activeSubscription != null && activeSubscription.Payments.Any(p => p.TransactionId != null && p.TransactionId.StartsWith("ManualActive"));
                    
                    var latestSub = subscriptions.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
                    var isManualExpiredOrCancelled = latestSub != null && latestSub.Payments.Any(p => p.TransactionId != null && p.TransactionId.StartsWith("ManualActive"));

                    return new UserManagementDto
                    {
                        Id = u.Id,
                        FullName = $"{u.FirstName} {u.LastName}".Trim(),
                        Email = u.Email ?? string.Empty,
                        PhoneNumber = u.PhoneNumber ?? string.Empty,
                        RegisterDate = u.CreatedAt,
                        IsActive = u.IsActive,
                        // Membership is now determined by subscriptions, not MemberProfile existence
                        IsMember = DetermineMembershipStatus(subscriptions),
                        SubscriptionStatus = DetermineSubscriptionStatus(subscriptions),
                        IsManualActive = isManualActive,
                        IsManualExpiredOrCancelled = isManualExpiredOrCancelled
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
                    RegisterDate = user.CreatedAt,

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
                        .ToList(),

                    IsManualActive = activeSubscription != null && activeSubscription.Payments.Any(p => p.TransactionId != null && p.TransactionId.StartsWith("ManualActive")),
                    IsManualExpiredOrCancelled = subscriptions.OrderByDescending(s => s.CreatedAt).FirstOrDefault() != null &&
                                                 subscriptions.OrderByDescending(s => s.CreatedAt).FirstOrDefault()!.Payments.Any(p => p.TransactionId != null && p.TransactionId.StartsWith("ManualActive"))
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

                var subscriptions = user.MemberProfile?.Subscriptions?.ToList() ?? new List<ArenaDomain.Entities.Subscription.UserSubscription>();
                var activeSubscription = subscriptions.FirstOrDefault(s => s.Status == SubscriptionStatus.Active);
                var isManualActive = activeSubscription != null && activeSubscription.Payments.Any(p => p.TransactionId != null && p.TransactionId.StartsWith("ManualActive"));

                var plans = await _planRepository.FindAsync(p => p.IsActive);

                var dto = new UserManagementDetailsDto
                {
                    Id = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    Email = user.Email ?? string.Empty,
                    IsActive = user.IsActive,

                    HasActiveSubscription = activeSubscription != null,
                    CurrentSubscriptionId = activeSubscription?.Id,
                    CurrentPlanNameEn = activeSubscription?.Plan?.NameEn,
                    CurrentPlanNameAr = activeSubscription?.Plan?.NameAr,
                    IsManualActive = isManualActive,
                    AvailablePlans = plans.Select(p => new SubscriptionPlanSelectionDto
                    {
                        Id = p.Id,
                        NameEn = p.NameEn,
                        NameAr = p.NameAr,
                        Price = p.Price,
                        DurationMonths = p.DurationMonths,
                        HasAI = p.HasAI
                    }).ToList()
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

            var active = subscriptions.FirstOrDefault(s => s.Status == SubscriptionStatus.Active);
            if (active != null)
                return SubscriptionStatus.Active;

            var latest = subscriptions.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
            return latest?.Status;
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
                CreatedAt = subscription.CreatedAt,
                IsManualActive = subscription.Payments.Any(p => p.TransactionId != null && p.TransactionId.StartsWith("ManualActive"))
            };
        }

        public async Task<Result<bool>> AddManualSubscription(Guid userId, Guid planId, string adminName)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null || user.IsDeleted)
                {
                    return Result<bool>.Failure(_localizer["UserNotFound"]);
                }

                var plan = await _planRepository.GetByIdAsync(planId);
                if (plan == null || !plan.IsActive)
                {
                    return Result<bool>.Failure(_localizer["PlanNotFoundOrInactive"]);
                }

                // If user has an active subscription, we cannot add a new one manually
                var subscriptions = user.MemberProfile?.Subscriptions ?? new List<ArenaDomain.Entities.Subscription.UserSubscription>();
                if (subscriptions.Any(s => s.Status == SubscriptionStatus.Active))
                {
                    return Result<bool>.Failure(_localizer["UserHasActiveSubscription"]);
                }

                // Activate the user's account status when a subscription is manually added
                user.IsActive = true;

                // If user doesn't have a member profile, create one
                var memberProfile = user.MemberProfile;
                if (memberProfile == null)
                {
                    memberProfile = new MemberProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        DateOfBirth = DateTime.UtcNow.AddYears(-20), // Default date of birth
                        Gender = Gender.Male, // Default gender
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = adminName
                    };
                    await _memberProfileRepository.AddAsync(memberProfile);
                    await _unitOfWork.SaveChangesAsync();
                }

                // Create manual active subscription
                var newSub = new ArenaDomain.Entities.Subscription.UserSubscription
                {
                    Id = Guid.NewGuid(),
                    MemberProfileId = memberProfile.Id,
                    PlanId = plan.Id,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(plan.DurationMonths),
                    Status = SubscriptionStatus.Active,
                    RemainingSessions = plan.SessionLimit ?? 0,
                    ReminderSent = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = adminName
                };

                await _subscriptionRepository.AddAsync(newSub);

                // Create associated cash payment record with transaction id "ManualActive"
                var payment = new ArenaDomain.Entities.Payments.Payment
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    UserSubscriptionId = newSub.Id,
                    Amount = plan.Price,
                    Currency = "EGP",
                    PaymentMethod = PaymentMethod.Cash,
                    TransactionId = $"ManualActive-{newSub.Id}",
                    Status = PaymentStatus.Paid,
                    PaymentDate = DateTime.UtcNow,
                    GatewayResponse = "Manual Activation by Admin",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = adminName
                };

                await _paymentRepository.AddAsync(payment);

                await _unitOfWork.SaveChangesAsync();
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
        }

        public async Task<Result<bool>> CancelActiveSubscription(Guid userId, Guid subscriptionId, string adminName)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null || user.IsDeleted)
                {
                    return Result<bool>.Failure(_localizer["UserNotFound"]);
                }

                var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
                if (subscription == null || subscription.IsDeleted)
                {
                    return Result<bool>.Failure(_localizer["SubscriptionNotFound"]);
                }

                if (subscription.Status != SubscriptionStatus.Active)
                {
                    return Result<bool>.Failure(_localizer["SubscriptionNotActive"]);
                }

                subscription.Status = SubscriptionStatus.Cancelled;
                subscription.EndDate = DateTime.UtcNow;
                subscription.UpdatedAt = DateTime.UtcNow;
                subscription.UpdatedBy = adminName;

                await _subscriptionRepository.UpdateAsync(subscription);
                await _unitOfWork.SaveChangesAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
        }
    }
}

