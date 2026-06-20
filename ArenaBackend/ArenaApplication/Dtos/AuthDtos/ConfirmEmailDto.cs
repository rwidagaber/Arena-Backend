using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.AuthDtos
{
    // Dtos/AuthDtos/ConfirmEmailDto.cs
    public class ConfirmEmailDto
    {
        public Guid UserId { get; set; }
        public string Otp { get; set; }
    }
}
