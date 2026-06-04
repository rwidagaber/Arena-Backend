using ArenaApplication.Dtos.UserSubscription;
using ArenaApplication.Services.UserSubscription;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArenaApi.Controllers
{
    [ApiController]
    [Route("api/user-subscriptions")]
    public class UserSubscriptionsController : ControllerBase
    {
        private readonly IUserSubscriptionService _userSubscriptionService;

        public UserSubscriptionsController(IUserSubscriptionService userSubscriptionService)
        {
            _userSubscriptionService = userSubscriptionService;
        }

        /// <summary>
        /// Get all user subscriptions
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserSubscriptionDto>>> GetAll(CancellationToken cancellationToken)
        {
            try
            {
                var subscriptions = await _userSubscriptionService.GetAllAsync(cancellationToken);
                return Ok(subscriptions);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving user subscriptions.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get a user subscription by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<UserSubscriptionDto>> GetById(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var subscription = await _userSubscriptionService.GetByIdAsync(id, cancellationToken);
                return Ok(subscription);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving the user subscription.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get user subscriptions by member profile ID
        /// </summary>
        [HttpGet("member/{memberProfileId}")]
        public async Task<ActionResult<IEnumerable<UserSubscriptionDto>>> GetByMemberId(Guid memberProfileId, CancellationToken cancellationToken)
        {
            try
            {
                var subscriptions = await _userSubscriptionService.GetByMemberIdAsync(memberProfileId, cancellationToken);
                return Ok(subscriptions);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving user subscriptions.", details = ex.Message });
            }
        }

        /// <summary>
        /// Create a new user subscription
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<UserSubscriptionDto>> Create([FromBody] CreateUserSubscriptionDto createDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var subscription = await _userSubscriptionService.CreateAsync(createDto, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = subscription.Id }, subscription);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while creating the user subscription.", details = ex.Message });
            }
        }

        /// <summary>
        /// Update the status of a user subscription
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<ActionResult<UserSubscriptionDto>> UpdateStatus(Guid id, [FromBody] UpdateUserSubscriptionStatusDto updateDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var subscription = await _userSubscriptionService.UpdateStatusAsync(id, updateDto, cancellationToken);
                return Ok(subscription);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while updating the user subscription status.", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a user subscription
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                await _userSubscriptionService.DeleteAsync(id, cancellationToken);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while deleting the user subscription.", details = ex.Message });
            }
        }
    }
}
