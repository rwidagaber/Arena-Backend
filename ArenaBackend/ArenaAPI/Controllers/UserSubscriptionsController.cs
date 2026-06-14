using ArenaApplication.Dtos.UserSubscription;
using ArenaApplication.IServices;
using ArenaApplication.Services.UserSubscription;
using ArenaDomain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ArenaApi.Controllers
{
    [ApiController]
    [Route("api/user-subscriptions")]
    public class UserSubscriptionsController : ControllerBase
    {
        private readonly IUserSubscriptionService _userSubscriptionService;
        private readonly IAnalyticsCacheVersionService _analyticsCacheVersionService;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public UserSubscriptionsController(
            IUserSubscriptionService userSubscriptionService,
            IAnalyticsCacheVersionService analyticsCacheVersionService,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _userSubscriptionService = userSubscriptionService;
            _analyticsCacheVersionService = analyticsCacheVersionService;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken,
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null)
        {
            try
            {
                if (page.HasValue || pageSize.HasValue)
                {
                    var result = await _userSubscriptionService.GetAllPagedAsync(
                        page ?? 1,
                        pageSize ?? 10,
                        cancellationToken);
                    return Ok(result);
                }

                var subscriptions = await _userSubscriptionService.GetAllAsync(cancellationToken);
                return Ok(subscriptions);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = _localizer["AnErrorOccurredRetrievingUserSubscriptions"], details = ex.Message });
            }
        }

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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = _localizer["AnErrorOccurredRetrievingUserSubscription"], details = ex.Message });
            }
        }

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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = _localizer["AnErrorOccurredRetrievingUserSubscriptions"], details = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<UserSubscriptionDto>> Create([FromBody] CreateUserSubscriptionDto createDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var subscription = await _userSubscriptionService.CreateAsync(createDto, cancellationToken);
                _analyticsCacheVersionService.BumpVersion();
                return CreatedAtAction(nameof(GetById), new { id = subscription.Id }, subscription);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = _localizer["AnErrorOccurredCreatingUserSubscription"], details = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<ActionResult<UserSubscriptionDto>> UpdateStatus(Guid id, [FromBody] UpdateUserSubscriptionStatusDto updateDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var subscription = await _userSubscriptionService.UpdateStatusAsync(id, updateDto, cancellationToken);
                _analyticsCacheVersionService.BumpVersion();
                return Ok(subscription);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = _localizer["AnErrorOccurredUpdatingUserSubscriptionStatus"], details = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                await _userSubscriptionService.DeleteAsync(id, cancellationToken);
                _analyticsCacheVersionService.BumpVersion();
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = _localizer["AnErrorOccurredDeletingUserSubscription"], details = ex.Message });
            }
        }
    }
}
