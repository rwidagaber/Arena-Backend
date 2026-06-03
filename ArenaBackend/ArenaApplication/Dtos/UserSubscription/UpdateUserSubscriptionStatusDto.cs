using ArenaDomain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ArenaApplication.Dtos.UserSubscription
{
    public class UpdateUserSubscriptionStatusDto
    {
        [Required]
        public SubscriptionStatus Status { get; set; }
    }
}
