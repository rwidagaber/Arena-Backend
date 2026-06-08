using ArenaApplication.Dtos.ProfileDtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.AuthDtos
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public string Role { get; set; } = null!;
        public Guid MemberProfileId { get; set; }   // ← add
        public bool IsSubscribed { get; set; }
        public GetProfileDto? Profile { get; set; }

        public bool IsGoogleUser { get; set; }

    }
}
