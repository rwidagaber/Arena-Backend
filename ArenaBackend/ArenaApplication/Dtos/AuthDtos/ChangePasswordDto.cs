using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.AuthDtos
{
    public class ChangePasswordDto
    {
        public string OldPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
        public string ConfirmNewPassword { get; set; } = null!;
    
    }
}
