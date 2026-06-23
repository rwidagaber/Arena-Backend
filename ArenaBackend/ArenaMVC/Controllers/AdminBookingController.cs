using ArenaApplication.Dtos.Booking;
using ArenaApplication.IServices;
using ArenaDomain.Entities;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using ArenaMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArenaMVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminBookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IGenericRepository<MemberProfile, Guid> _memberProfileRepo;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public AdminBookingController(
            IBookingService bookingService,
            IGenericRepository<MemberProfile, Guid> memberProfileRepo,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _bookingService = bookingService;
            _memberProfileRepo = memberProfileRepo;
            _localizer = localizer;
        }

        private const int DefaultPageSize = 10;

        [HttpGet]
        public async Task<IActionResult> Index(BookingStatus? status, DateTime? bookingDate, int page = 1, int pageSize = DefaultPageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = DefaultPageSize;

            var result = await _bookingService.GetAdminBookings(status, bookingDate, page, pageSize);
            if (!result.IsSuccess)
                return View("Error");

            var pagedResult    = result.Value;
            var bookings       = pagedResult.Items;
            var profilesMap    = await BuildProfileNameMapAsync(bookings.Select(b => b.MemberProfileId));
            var viewModels     = MapBookings(bookings, profilesMap);

            var viewModel = new AdminBookingPagedViewModel
            {
                Items          = viewModels,
                TotalCount     = pagedResult.TotalCount,
                Page           = page,
                PageSize       = pageSize,
                SelectedStatus = status,
                SelectedDate   = bookingDate
            };

            ViewBag.SelectedStatus = status;
            ViewBag.SelectedDate   = bookingDate?.Date;
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Today()
        {
            var result = await _bookingService.GetTodaySchedule();
            if (!result.IsSuccess)
                return View("Error");

            var bookings    = result.Value;
            var profilesMap = await BuildProfileNameMapAsync(bookings.Select(b => b.MemberProfileId));
            var viewModels  = MapBookings(bookings, profilesMap);

            return View(viewModels);
        }
        // ── Private helpers ────────────────────────────────────────────────

        private async Task<Dictionary<Guid, string>> BuildProfileNameMapAsync(
            IEnumerable<Guid> memberProfileIds)
        {
            var ids      = memberProfileIds.Distinct().ToList();
            var profiles = await _memberProfileRepo.GetAll()
                .Include(mp => mp.User)
                .Where(mp => ids.Contains(mp.Id))
                .ToListAsync();

            return profiles.ToDictionary(
                mp => mp.Id,
                mp => mp.User != null
                    ? $"{mp.User.FirstName} {mp.User.LastName}".Trim()
                    : string.Empty);
        }

        private static List<AdminBookingViewModel> MapBookings(
            IEnumerable<BookingDto> bookings,
            Dictionary<Guid, string> profilesMap)
        {
            return bookings.Select(b => new AdminBookingViewModel
            {
                Id                = b.Id,
                MemberProfileName = profilesMap.TryGetValue(b.MemberProfileId, out var name)
                                    && !string.IsNullOrWhiteSpace(name)
                                        ? name
                                        : b.MemberProfileId.ToString(),
                BookingDate = b.BookingDate,
                StartTime   = b.StartTime,
                EndTime     = b.EndTime,
                Status      = b.Status
            }).ToList();
        }
    }
}
