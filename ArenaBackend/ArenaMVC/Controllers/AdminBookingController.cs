using ArenaApplication.Dtos.Booking;
using ArenaApplication.IServices;
using ArenaDomain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ArenaMVC.Controllers
{
    public class AdminBookingController : Controller
    {
        private readonly IBookingService _bookingService;

        public AdminBookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(BookingStatus? status, DateTime? bookingDate)
        {
            var result = await _bookingService.GetAdminBookings(status, bookingDate);
            if (!result.IsSuccess)
                return View("Error");

            ViewBag.SelectedStatus = status;
            ViewBag.SelectedDate = bookingDate?.Date;
            return View(result.Value);
        }

        [HttpGet]
        public async Task<IActionResult> Today()
        {
            var result = await _bookingService.GetTodaySchedule();
            if (!result.IsSuccess)
                return View("Error");

            return View(result.Value);
        }
    }
}
