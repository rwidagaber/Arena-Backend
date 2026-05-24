using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.loginDto
{
    public class loginDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        //public bool RememberMe { get; set; } = false;
    }
}
