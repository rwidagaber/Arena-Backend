using ArenaDomain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.Booking
{
     public class BookingDto
    {
        public Guid Id { get; set; }

        public Guid MemberProfileId { get; set; }

        public Guid GymId { get; set; }

        public Guid? TrainerId { get; set; }

        public DateTime BookingDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        public BookingStatus Status { get; set; }
    }

}
