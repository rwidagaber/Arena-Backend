using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using ArenaDomain.Entities.Notifications;
using ArenaDomain.Entities.Payments;
namespace ArenaDomain.Entities.User

{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string PreferredLanguage { get; set; } 

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual MemberProfile? MemberProfile { get; set; }

        //public virtual ICollection<Notification> Notifications { get; set; } = [];

        public virtual ICollection<Payment> Payments { get; set; } = [];

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
