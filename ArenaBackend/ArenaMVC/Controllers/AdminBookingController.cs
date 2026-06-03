using ArenaApplication.Dtos.Booking;
using ArenaApplication.IServices;
using ArenaDomain.Entities;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using ArenaMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArenaMVC.Controllers
{
    public class AdminBookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IGenericRepository<MemberProfile, Guid> _memberProfileRepo;

        public AdminBookingController(
            IBookingService bookingService,
            IGenericRepository<MemberProfile, Guid> memberProfileRepo)
        {
            _bookingService = bookingService;
            _memberProfileRepo = memberProfileRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Index(BookingStatus? status, DateTime? bookingDate)
        {
            var result = await _bookingService.GetAdminBookings(status, bookingDate);
            if (!result.IsSuccess)
                return View("Error");

            var bookings = result.Value;
            var memberProfileIds = bookings.Select(b => b.MemberProfileId).Distinct().ToList();

            var profiles = await _memberProfileRepo.GetAll()
                .Include(mp => mp.User)
                .Where(mp => memberProfileIds.Contains(mp.Id))
                .ToListAsync();

            var profilesMap = profiles.ToDictionary(
                mp => mp.Id,
                mp => mp.User != null ? $"{mp.User.FirstName} {mp.User.LastName}".Trim() : string.Empty
            );

            var viewModels = bookings.Select(b => new AdminBookingViewModel
            {
                Id = b.Id,
                MemberProfileName = profilesMap.TryGetValue(b.MemberProfileId, out var name) && !string.IsNullOrWhiteSpace(name) 
                    ? name 
                    : b.MemberProfileId.ToString(),
                BookingDate = b.BookingDate,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                Status = b.Status
            }).ToList();

            ViewBag.SelectedStatus = status;
            ViewBag.SelectedDate = bookingDate?.Date;
            return View(viewModels);
        }

        [HttpGet]
        public async Task<IActionResult> Today()
        {
            var result = await _bookingService.GetTodaySchedule();
            if (!result.IsSuccess)
                return View("Error");

            var bookings = result.Value;
            var memberProfileIds = bookings.Select(b => b.MemberProfileId).Distinct().ToList();

            var profiles = await _memberProfileRepo.GetAll()
                .Include(mp => mp.User)
                .Where(mp => memberProfileIds.Contains(mp.Id))
                .ToListAsync();

            var profilesMap = profiles.ToDictionary(
                mp => mp.Id,
                mp => mp.User != null ? $"{mp.User.FirstName} {mp.User.LastName}".Trim() : string.Empty
            );

            var viewModels = bookings.Select(b => new AdminBookingViewModel
            {
                Id = b.Id,
                MemberProfileName = profilesMap.TryGetValue(b.MemberProfileId, out var name) && !string.IsNullOrWhiteSpace(name) 
                    ? name 
                    : b.MemberProfileId.ToString(),
                BookingDate = b.BookingDate,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                Status = b.Status
            }).ToList();

            return View(viewModels);
        }
    }
}
