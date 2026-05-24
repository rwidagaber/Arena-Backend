using System.ComponentModel.DataAnnotations;

namespace ArenaApplication.Dtos.UserSubscription
{
    public class CreateUserSubscriptionDto
    {
        [Required]
        public Guid MemberProfileId { get; set; }

        [Required]
        public Guid SubscriptionPlanId { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
    }
}