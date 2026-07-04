using System;

namespace ArenaMVC.Models
{
    public class ManageUserViewModel
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        // Subscription properties
        public bool HasActiveSubscription { get; set; }
        public Guid? CurrentSubscriptionId { get; set; }
        public string? CurrentPlanName { get; set; }
        public string? CurrentSubscriptionStatus { get; set; }
        public bool IsManualActive { get; set; }
        public List<SubscriptionPlanSelectionViewModel> AvailablePlans { get; set; } = new();

        // Form post capture properties
        public Guid? SelectedPlanId { get; set; }
        public string? SubmitAction { get; set; }
    }

    public class SubscriptionPlanSelectionViewModel
    {
        public Guid Id { get; set; }
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationMonths { get; set; }
        public bool HasAI { get; set; }
    }
}
