using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.LoginDto
{
    public class UserloginDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;

        //Session Cookie
        public bool RememberMe { get; set; } = false;
    }
}
