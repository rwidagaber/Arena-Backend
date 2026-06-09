using ArenaApplication.Dtos.SubscriptionPlanDtos;
using ArenaApplication.Services.SubscriptionPlan;
using ArenaDomain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ArenaApi.Controllers
{
    [ApiController]
    [Route("api/subscription-plans")]
    public class SubscriptionPlansController : ControllerBase
    {
        private readonly ISubscriptionPlanService _subscriptionPlanService;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public SubscriptionPlansController(
            ISubscriptionPlanService subscriptionPlanService,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _subscriptionPlanService = subscriptionPlanService;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubscriptionPlanDto>>> GetAll(CancellationToken cancellationToken)
        {
            try
            {
                var plans = await _subscriptionPlanService.GetAllAsync(cancellationToken);
                return Ok(plans);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = _localizer["AnErrorOccurredRetrievingSubscriptionPlans"], details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SubscriptionPlanDto>> GetById(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var plan = await _subscriptionPlanService.GetByIdAsync(id, cancellationToken);
                return Ok(plan);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = _localizer["AnErrorOccurredRetrievingSubscriptionPlan"], details = ex.Message });
            }
        }
    }
}
