using ArenaDomain.Entities;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ArenaApplication.Dtos.Booking
{
    public class CreateBookingDto
    {
        [Required]
        public Guid MemberProfileId { get; set; }

        [Required]
        public Guid GymId { get; set; }

        public Guid? TrainerId { get; set; }

        [Required]
        public DateTime BookingDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

    }
}
