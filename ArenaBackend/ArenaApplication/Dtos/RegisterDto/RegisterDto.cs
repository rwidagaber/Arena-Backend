using ArenaDomain.Entities.Subscription;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.RegisterDto
{
    public class RegisterDto
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;

        public string ConfirmPassword { get; set; } = null!;

       //Navigation Properties
       public SubscriptionPlan SelectedPlan { get; set; } = null!;
    }
}
