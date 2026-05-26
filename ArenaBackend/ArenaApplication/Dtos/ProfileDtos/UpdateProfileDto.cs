using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.ProfileDtos
{
    public class UpdateProfileDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? PreferredLanguage { get; set; }

        public double? Weight { get; set; }
        public double? Height { get; set; }
        public string? Gender { get; set; }
        public string? ProfileImage { get; set; }
        public DateOnly? Birthday { get; set; }
    }
}
