using ArenaApplication.Dtos.SubscriptionPlanDtos;
using ArenaApplication.Services.SubscriptionPlan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArenaApi.Controllers
{
    [ApiController]
    [Route("api/subscription-plans")]
    public class SubscriptionPlansController : ControllerBase
    {
        private readonly ISubscriptionPlanService _subscriptionPlanService;

        public SubscriptionPlansController(ISubscriptionPlanService subscriptionPlanService)
        {
            _subscriptionPlanService = subscriptionPlanService;
        }

        /// <summary>
        /// Get all subscription plans
        /// </summary>
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving subscription plans.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get a subscription plan by ID
        /// </summary>
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving the subscription plan.", details = ex.Message });
            }
        }
    }
}
