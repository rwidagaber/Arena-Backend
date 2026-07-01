using ArenaApplication.IServices;
using ArenaApi.Configurations.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ArenaApi.Controllers
{
    [ApiController]
    [Route("api/nutritionplans")]
    [Authorize]
    [TypeFilter(typeof(RequireAIPlanFilter))]
    public class NutritionPlanActivationController : ControllerBase
    {
        private readonly INutritionPlanActivationService _activationService;
        private readonly ICurrentUserService _currentUserService;

        public NutritionPlanActivationController(
            INutritionPlanActivationService activationService,
            ICurrentUserService currentUserService)
        {
            _activationService = activationService;
            _currentUserService = currentUserService;
        }

        [HttpPut("{id}/activate")]
        public async Task<IActionResult> Activate(Guid id)
        {
            var memberProfileId = _currentUserService.MemberProfileId;
            var result = await _activationService.ActivateAsync(memberProfileId, id);
            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            return NoContent();
        }

        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            var memberProfileId = _currentUserService.MemberProfileId;
            var result = await _activationService.DeactivateAsync(memberProfileId, id);
            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            return NoContent();
        }
    }
}
