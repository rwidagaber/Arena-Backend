using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.QrCodeDtos
{
    public class QrScanResultDto
    {
     

        public bool IsExpired { get; set; }

        public bool IsAlreadyUsed { get; set; }

        public string Message { get; set; } = string.Empty;

        public Guid? BookingId { get; set; }

        public Guid? MemberProfileId { get; set; }

        public string? MemberName { get; set; }

        public DateTime? SessionDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }
    }
}
