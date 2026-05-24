using ArenaDomain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ArenaApplication.Dtos.Booking
{
    public class UpdateBookingDto
    {
        [Required]
        public Guid Id { get; set; }

        public DateTime BookingDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        public BookingStatus Status { get; set; }
    }
}
