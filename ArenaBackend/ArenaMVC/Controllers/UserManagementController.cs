using ArenaApplication.IServices;
using ArenaMVC.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArenaMVC.Controllers
{
    public class UserManagementController : Controller
    {
        private readonly IUserManagementService _userService;

        public UserManagementController(IUserManagementService userService)
        {
            _userService = userService;
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
                    TempData["Error"] = result.Errors != null ? string.Join(", ", result.Errors) : "An error occurred.";
                    return View(new List<UserListViewModel>());
                }

                var viewModels = result.Value.Select(u => new UserListViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    RegisterDate = u.RegisterDate,
                    IsActive = u.IsActive
                }).ToList();

                ViewBag.SearchString = search;
                return View(viewModels);
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while retrieving users.";
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
                    TempData["Error"] = result.Errors != null ? string.Join(", ", result.Errors) : "An error occurred.";
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
                    RegisterDate = user.RegisterDate
                };

                return View(viewModel);
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while retrieving user details.";
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
                    TempData["Error"] = result.Errors != null ? string.Join(", ", result.Errors) : "An error occurred.";
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
                TempData["Error"] = "An error occurred while loading the manage user page.";
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
                    TempData["Error"] = "Invalid user ID.";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _userService.UpdateUserStatus(id, model.IsActive);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.Errors != null ? string.Join(", ", result.Errors) : "An error occurred.";
                    return View(model);
                }

                TempData["Success"] = "User status updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while saving user status changes.";
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
                    TempData["Error"] = result.Errors != null ? string.Join(", ", result.Errors) : "An error occurred.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Success"] = "User deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while deleting the user.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
