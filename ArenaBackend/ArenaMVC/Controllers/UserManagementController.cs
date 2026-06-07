using ArenaApplication.Dtos.UserManagement;
using ArenaApplication.IServices;
using ArenaDomain.Shared;
using ArenaMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArenaMVC.Controllers
{
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

        // GET: UserManagement
        [HttpGet]
        public async Task<IActionResult> Index(string search)
        {
            try
            {
                var result = await _userService.GetUsers(search);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.Errors != null ? string.Join(", ", result.Errors) : _localizer["AnErrorOccurredRetrievingUsers"];
                    return View(new List<UserListViewModel>());
                }

                var viewModels = result.Value.Select(u => new UserListViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    RegisterDate = u.RegisterDate,
                    IsActive = u.IsActive,
                    IsMember = u.IsMember,
                    SubscriptionStatus = u.SubscriptionStatus
                }).ToList();

                ViewBag.SearchString = search;
                return View(viewModels);
            }
            catch (Exception)
            {
                TempData["Error"] = _localizer["AnErrorOccurredRetrievingUsers"];
                return View(new List<UserListViewModel>());
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
                    TempData["Error"] = result.Errors != null ? string.Join(", ", result.Errors) : _localizer["AnErrorOccurredRetrievingUserDetails"];
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
                        .ToList()
                };

                return View(viewModel);
            }
            catch (Exception)
            {
                TempData["Error"] = _localizer["AnErrorOccurredRetrievingUserDetails"];
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
                    TempData["Error"] = result.Errors != null ? string.Join(", ", result.Errors) : _localizer["AnErrorOccurredLoadingManageUser"];
                    return RedirectToAction(nameof(Index));
                }

                var user = result.Value;
                var viewModel = new ManageUserViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    IsActive = user.IsActive
                };

                return View(viewModel);
            }
            catch (Exception)
            {
                TempData["Error"] = _localizer["AnErrorOccurredLoadingManageUser"];
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
                    TempData["Error"] = _localizer["InvalidUserId"];
                    return RedirectToAction(nameof(Index));
                }

                var result = await _userService.UpdateUserStatus(id, model.IsActive);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.Errors != null ? string.Join(", ", result.Errors) : _localizer["AnErrorOccurredSavingUserStatus"];
                    return View(model);
                }

                TempData["Success"] = _localizer["UserStatusUpdatedSuccessfully"];
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Error"] = _localizer["AnErrorOccurredSavingUserStatus"];
                return View(model);
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
                    TempData["Error"] = result.Errors != null ? string.Join(", ", result.Errors) : _localizer["AnErrorOccurredDeletingUser"];
                    return RedirectToAction(nameof(Index));
                }

                TempData["Success"] = _localizer["UserDeletedSuccessfully"];
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Error"] = _localizer["AnErrorOccurredDeletingUser"];
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
                CreatedAt = dto.CreatedAt
            };
        }
    }
}
