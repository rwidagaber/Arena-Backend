using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Shared
{
    public class JWTSettings
    {
        public const string SectionName = "Jwt";
        public string Key { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public int AccessTokenExpiryMinutes { get; set; }
        public int RefreshTokenExpiryDays { get; set; }
    }
}
