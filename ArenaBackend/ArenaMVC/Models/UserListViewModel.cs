using System;

namespace ArenaMVC.Models
{
    public class UserListViewModel
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? RegisterDate { get; set; }
        public bool IsActive { get; set; }
    }
}
