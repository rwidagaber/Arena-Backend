using System;

namespace ArenaApplication.Dtos.UserManagement
{
    public class UserManagementDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? RegisterDate { get; set; }
        public bool IsActive { get; set; }
    }
}
