using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Entities.Health
{
    public class ProgressLog : BaseEntity<Guid>
    {
        public Guid MemberProfileId { get; set; }

        public virtual MemberProfile MemberProfile { get; set; } = null!;

        public decimal Weight { get; set; }

        public decimal? BodyFat { get; set; }

        public decimal? MuscleMass { get; set; }

        public DateTime LoggedAt { get; set; }
    }
}
