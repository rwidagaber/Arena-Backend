using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.confirmDto
{
    public class ConfirmEmailDto
    {
        public string Otp { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
