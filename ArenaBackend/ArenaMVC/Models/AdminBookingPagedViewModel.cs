using ArenaDomain.Enums;
using System;
using System.Collections.Generic;

namespace ArenaMVC.Models
{
    public class AdminBookingPagedViewModel
    {
        public IEnumerable<AdminBookingViewModel> Items { get; set; } = Enumerable.Empty<AdminBookingViewModel>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
        public BookingStatus? SelectedStatus { get; set; }
        public DateTime? SelectedDate { get; set; }
    }
}
