using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.RegisterDto
{
    public class UserRegisterDto
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;

        public string PhoneNumber { get; set; } = null!;
        public DateOnly Birthday { get; set; }
 


    }
}
