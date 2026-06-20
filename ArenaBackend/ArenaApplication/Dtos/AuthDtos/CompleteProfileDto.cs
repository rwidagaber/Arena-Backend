using ArenaDomain.Enums;

namespace ArenaApplication.Dtos.AuthDtos
{
    public class CompleteProfileDto
    {
        public string PhoneNumber { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public decimal Weight { get; set; }
        public decimal Height { get; set; }
        public Gender Gender { get; set; }
    }
}