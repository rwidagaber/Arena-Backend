using ArenaDomain.Entities.Subscription;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;

namespace ArenaApi.Configurations.Filters
{
    /// <summary>
    /// Action filter that blocks access to AI features unless the current user
    /// has at least one active subscription plan with <see cref="SubscriptionPlan.HasAI"/> enabled.
    /// Usage: [TypeFilter(typeof(RequireAIPlanFilter))]
    /// </summary>
    public class RequireAIPlanFilter : IAsyncActionFilter
    {
        private readonly IGenericRepository<UserSubscription, Guid> _subscriptionRepo;
        private readonly IGenericRepository<SubscriptionPlan, Guid> _planRepo;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public RequireAIPlanFilter(
            IGenericRepository<UserSubscription, Guid> subscriptionRepo,
            IGenericRepository<SubscriptionPlan, Guid> planRepo,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _subscriptionRepo = subscriptionRepo;
            _planRepo = planRepo;
            _localizer = localizer;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var memberProfileIdClaim = context.HttpContext.User.FindFirst("memberProfileId")?.Value;

            if (string.IsNullOrWhiteSpace(memberProfileIdClaim) || !Guid.TryParse(memberProfileIdClaim, out var memberProfileId))
            {
                context.Result = new UnauthorizedObjectResult(new { message = "Unauthorized" });
                return;
            }

            var activeSubscriptions = await _subscriptionRepo.FindAsync(
                s => s.MemberProfileId == memberProfileId
                     && !s.IsDeleted
                     && s.EndDate > DateTime.UtcNow);

            var hasAIPlan = false;
            foreach (var subscription in activeSubscriptions)
            {
                var plan = await _planRepo.GetByIdAsync(subscription.PlanId);
                if (plan is { HasAI: true })
                {
                    hasAIPlan = true;
                    break;
                }
            }

            if (!hasAIPlan)
            {
                context.Result = new ObjectResult(new { message = _localizer["AIFeatureRequiresAIPlan"].Value })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            await next();
        }
    }
}
