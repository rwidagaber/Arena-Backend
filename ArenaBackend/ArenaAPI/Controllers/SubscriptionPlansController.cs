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

        /// <summary>
        /// Create a new subscription plan (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SubscriptionPlanDto>> Create([FromBody] SubscriptionPlanDto createDto, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var createdPlan = await _subscriptionPlanService.CreateAsync(createDto, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = createdPlan.Id }, createdPlan);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while creating the subscription plan.", details = ex.Message });
            }
        }

        /// <summary>
        /// Update a subscription plan (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SubscriptionPlanDto>> Update(Guid id, [FromBody] UpdateSubscriptionPlanDto updateDto, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var updatedPlan = await _subscriptionPlanService.UpdateAsync(id, updateDto, cancellationToken);
                return Ok(updatedPlan);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while updating the subscription plan.", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a subscription plan (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                await _subscriptionPlanService.DeleteAsync(id, cancellationToken);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while deleting the subscription plan.", details = ex.Message });
            }
        }
    }
}
