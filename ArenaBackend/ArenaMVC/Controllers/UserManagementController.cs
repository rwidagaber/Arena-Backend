using ArenaApplication.Dtos.UserManagement;
using ArenaApplication.IServices;
using ArenaDomain.Shared;
using ArenaMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArenaMVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly IUserManagementService _userService;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public UserManagementController(
            IUserManagementService userService,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _userService = userService;
            _localizer = localizer;
        }

        private const int DefaultPageSize = 10;

        // GET: UserManagement/SearchPartial  – AJAX live-search endpoint
        [HttpGet]
        public async Task<IActionResult> SearchPartial(
            string? search, 
            bool? isActive, 
            ArenaDomain.Enums.MembershipStatus? membershipStatus, 
            string? subscriptionStatus, 
            int page = 1, 
            int pageSize = DefaultPageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = DefaultPageSize;

            try
            {
                var result = await _userService.GetUsers(search, isActive, membershipStatus, subscriptionStatus, page, pageSize);
                if (!result.IsSuccess)
                    return PartialView("_UserResults", new UserListPagedViewModel { Page = page, PageSize = pageSize, Search = search, IsActive = isActive, MembershipStatusFilter = membershipStatus, SubscriptionStatusFilter = subscriptionStatus });

                var pagedResult = result.Value;

                var viewModels = pagedResult.Items.Select(u => new UserListViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    RegisterDate = u.RegisterDate,
                    IsActive = u.IsActive,
                    IsMember = u.IsMember,
                    SubscriptionStatus = u.SubscriptionStatus,
                    IsManualActive = u.IsManualActive,
                    IsManualExpiredOrCancelled = u.IsManualExpiredOrCancelled
                }).ToList();

                var viewModel = new UserListPagedViewModel
                {
                    Items = viewModels,
                    TotalCount = pagedResult.TotalCount,
                    Page = page,
                    PageSize = pageSize,
                    Search = search,
                    IsActive = isActive,
                    MembershipStatusFilter = membershipStatus,
                    SubscriptionStatusFilter = subscriptionStatus
                };

                return PartialView("_UserResults", viewModel);
            }
            catch (Exception)
            {
                return PartialView("_UserResults", new UserListPagedViewModel { Page = page, PageSize = pageSize, Search = search, IsActive = isActive, MembershipStatusFilter = membershipStatus, SubscriptionStatusFilter = subscriptionStatus });
            }
        }

        // GET: UserManagement
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search, 
            bool? isActive, 
            ArenaDomain.Enums.MembershipStatus? membershipStatus, 
            string? subscriptionStatus, 
            int page = 1, 
            int pageSize = DefaultPageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = DefaultPageSize;

            try
            {
                var result = await _userService.GetUsers(search, isActive, membershipStatus, subscriptionStatus, page, pageSize);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.Errors != null ? string.Join(", ", result.Errors) : _localizer["AnErrorOccurredRetrievingUsers"].Value;
                    return View(new UserListPagedViewModel { Page = page, PageSize = pageSize, IsActive = isActive, MembershipStatusFilter = membershipStatus, SubscriptionStatusFilter = subscriptionStatus });
                }

                var pagedResult = result.Value;

                var viewModels = pagedResult.Items.Select(u => new UserListViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    RegisterDate = u.RegisterDate,
                    IsActive = u.IsActive,
                    IsMember = u.IsMember,
                    SubscriptionStatus = u.SubscriptionStatus,
                    IsManualActive = u.IsManualActive,
                    IsManualExpiredOrCancelled = u.IsManualExpiredOrCancelled
                }).ToList();

                var viewModel = new UserListPagedViewModel
                {
                    Items = viewModels,
                    TotalCount = pagedResult.TotalCount,
                    Page = page,
                    PageSize = pageSize,
                    Search = search,
                    IsActive = isActive,
                    MembershipStatusFilter = membershipStatus,
                    SubscriptionStatusFilter = subscriptionStatus
                };

                ViewBag.SearchString = search;
                return View(viewModel);
            }
            catch (Exception)
            {
                TempData["Error"] = _localizer["AnErrorOccurredRetrievingUsers"].Value;
                return View(new UserListPagedViewModel { Page = page, PageSize = pageSize, IsActive = isActive, MembershipStatusFilter = membershipStatus, SubscriptionStatusFilter = subscriptionStatus });
            }
        }

        // GET: UserManagement/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var result = await _userService.GetUserDetails(id);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.Errors != null ? string.Join(", ", result.Errors) : _localizer["AnErrorOccurredRetrievingUserDetails"].Value;
                    return RedirectToAction(nameof(Index));
                }

                var user = result.Value;
                var viewModel = new UserDetailsViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Username = user.Username,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    PreferredLanguage = user.PreferredLanguage,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    RegisterDate = user.RegisterDate,

                    // Membership Summary (Section 1)
                    MembershipStatus = user.MembershipStatus,
                    IsMember = user.IsMember,
                    MemberSince = user.MemberSince,
                    TotalSubscriptions = user.TotalSubscriptions,
                    CurrentSubscriptionStatus = user.CurrentSubscriptionStatus,

                    // Current Active Subscription (Section 2)
                    CurrentSubscription = user.CurrentSubscription != null ? MapSubscriptionItem(user.CurrentSubscription) : null,

                    // Subscription History (Section 3)
                    SubscriptionHistory = user.SubscriptionHistory
                        .Select(s => MapSubscriptionItem(s))
                        .ToList(),
                    IsManualActive = user.IsManualActive,
                    IsManualExpiredOrCancelled = user.IsManualExpiredOrCancelled
                };

                return View(viewModel);
            }
            catch (Exception)
            {
                TempData["Error"] = _localizer["AnErrorOccurredRetrievingUserDetails"].Value;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: UserManagement/Manage/{id}
        [HttpGet]
        public async Task<IActionResult> Manage(Guid id)
        {
            try
            {
                var result = await _userService.GetUserForManage(id);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.Errors != null ? string.Join(", ", result.Errors) : _localizer["AnErrorOccurredLoadingManageUser"].Value;
                    return RedirectToAction(nameof(Index));
                }

                var user = result.Value;
                var viewModel = new ManageUserViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    IsActive = user.IsActive,

                    HasActiveSubscription = user.HasActiveSubscription,
                    CurrentSubscriptionId = user.CurrentSubscriptionId,
                    CurrentPlanName = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName.StartsWith("ar", StringComparison.OrdinalIgnoreCase) 
                        ? user.CurrentPlanNameAr 
                        : user.CurrentPlanNameEn,
                    CurrentSubscriptionStatus = user.CurrentSubscriptionStatus?.ToString(),
                    IsManualActive = user.IsManualActive,
                    AvailablePlans = user.AvailablePlans.Select(p => new SubscriptionPlanSelectionViewModel
                    {
                        Id = p.Id,
                        NameEn = p.NameEn,
                        NameAr = p.NameAr,
                        Price = p.Price,
                        DurationMonths = p.DurationMonths,
                        HasAI = p.HasAI
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception)
            {
                TempData["Error"] = _localizer["AnErrorOccurredLoadingManageUser"].Value;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: UserManagement/Manage/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(Guid id, ManageUserViewModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    TempData["Error"] = _localizer["InvalidUserId"].Value;
                    return RedirectToAction(nameof(Index));
                }

                var adminName = User.Identity?.Name ?? "Admin";

                if (model.SubmitAction == "addSubscription")
                {
                    if (!model.SelectedPlanId.HasValue || model.SelectedPlanId.Value == Guid.Empty)
                    {
                        TempData["Error"] = _localizer["PleaseSelectPlan"].Value;
                        await PopulateSubscriptionData(id, model);
                        return View(model);
                    }

                    var result = await _userService.AddManualSubscription(id, model.SelectedPlanId.Value, adminName);
                    if (!result.IsSuccess)
                    {
                        TempData["Error"] = result.Errors != null ? string.Join(", ", result.Errors) : _localizer["AnErrorOccurredProcessingSubscription"].Value;
                        await PopulateSubscriptionData(id, model);
                        return View(model);
                    }

                    TempData["Success"] = _localizer["SubscriptionAddedSuccessfully"].Value;
                    return RedirectToAction(nameof(Index));
                }

                if (model.SubmitAction == "cancelSubscription")
                {
                    if (!model.CurrentSubscriptionId.HasValue || model.CurrentSubscriptionId.Value == Guid.Empty)
                    {
                        TempData["Error"] = _localizer["NoActiveSubscriptionToCancel"].Value;
                        await PopulateSubscriptionData(id, model);
                        return View(model);
                    }

                    var result = await _userService.CancelActiveSubscription(id, model.CurrentSubscriptionId.Value, adminName);
                    if (!result.IsSuccess)
                    {
                        TempData["Error"] = result.Errors != null ? string.Join(", ", result.Errors) : _localizer["AnErrorOccurredCancellingSubscription"].Value;
                        await PopulateSubscriptionData(id, model);
                        return View(model);
                    }

                    TempData["Success"] = _localizer["SubscriptionCancelledSuccessfully"].Value;
                    return RedirectToAction(nameof(Index));
                }

                // Default action: Update account status
                var statusResult = await _userService.UpdateUserStatus(id, model.IsActive);
                if (!statusResult.IsSuccess)
                {
                    TempData["Error"] = statusResult.Errors != null ? string.Join(", ", statusResult.Errors) : _localizer["AnErrorOccurredSavingUserStatus"].Value;
                    await PopulateSubscriptionData(id, model);
                    return View(model);
                }

                TempData["Success"] = _localizer["UserStatusUpdatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Error"] = _localizer["AnErrorOccurredSavingUserStatus"].Value;
                await PopulateSubscriptionData(id, model);
                return View(model);
            }
        }

        private async Task PopulateSubscriptionData(Guid id, ManageUserViewModel model)
        {
            var result = await _userService.GetUserForManage(id);
            if (result.IsSuccess)
            {
                var user = result.Value;
                model.FullName = user.FullName;
                model.Email = user.Email;
                model.HasActiveSubscription = user.HasActiveSubscription;
                model.CurrentSubscriptionId = user.CurrentSubscriptionId;
                model.CurrentPlanName = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName.StartsWith("ar", StringComparison.OrdinalIgnoreCase) 
                    ? user.CurrentPlanNameAr 
                    : user.CurrentPlanNameEn;
                model.CurrentSubscriptionStatus = user.CurrentSubscriptionStatus?.ToString();
                model.IsManualActive = user.IsManualActive;
                model.AvailablePlans = user.AvailablePlans.Select(p => new SubscriptionPlanSelectionViewModel
                {
                    Id = p.Id,
                    NameEn = p.NameEn,
                    NameAr = p.NameAr,
                    Price = p.Price,
                    DurationMonths = p.DurationMonths,
                    HasAI = p.HasAI
                }).ToList();
            }
        }

        // POST: UserManagement/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var result = await _userService.SoftDeleteUser(id);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.Errors != null ? string.Join(", ", result.Errors) : _localizer["AnErrorOccurredDeletingUser"].Value;
                    return RedirectToAction(nameof(Index));
                }

                TempData["Success"] = _localizer["UserDeletedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Error"] = _localizer["AnErrorOccurredDeletingUser"].Value;
                return RedirectToAction(nameof(Index));
            }
        }

        private static SubscriptionItemViewModel MapSubscriptionItem(UserSubscriptionItemDto dto)
        {
            return new SubscriptionItemViewModel
            {
                PlanName = dto.PlanName,
                Status = dto.Status,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                RemainingSessions = dto.RemainingSessions,
                DurationDays = dto.DurationDays,
                CreatedAt = dto.CreatedAt,
                IsManualActive = dto.IsManualActive
            };
        }
    }
}
