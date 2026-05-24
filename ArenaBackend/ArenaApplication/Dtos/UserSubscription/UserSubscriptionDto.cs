namespace ArenaApplication.Dtos.UserSubscription
{
    public class UserSubscriptionDto
    {
        public Guid Id { get; set; }
        public string MemberName { get; set; }
        public string PlanName { get; set; }
        public decimal PlanPrice { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public int RemainingSessions { get; set; }
        public bool ReminderSent { get; set; }
    }
    
}