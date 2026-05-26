using ArenaDomain.Entities.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Shared
{
    public class RefreshToken : BaseEntity<Guid>
    {
        public string Token { get; set; } = null!;
        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
    }
}
