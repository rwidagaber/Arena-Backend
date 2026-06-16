using ArenaApplication.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ArenaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NutritionPlansController : ControllerBase
    {
        private readonly INutritionPlanService _nutritionPlanService;
        private readonly ICurrentUserService _currentUserService;

        public NutritionPlansController(INutritionPlanService nutritionPlanService, ICurrentUserService currentUserService)
        {
            _nutritionPlanService = nutritionPlanService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyNutritionPlans()
        {
            var memberProfileId = _currentUserService.MemberProfileId;
            var result = await _nutritionPlanService.GetNutritionPlansByMemberIdAsync(memberProfileId);
            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            return Ok(result.Value);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetMyActiveNutritionPlan()
        {
            var memberProfileId = _currentUserService.MemberProfileId;
            var result = await _nutritionPlanService.GetActiveNutritionPlanByMemberIdAsync(memberProfileId);
            if (!result.IsSuccess)
                return NotFound(result.Errors);

            return Ok(result.Value);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNutritionPlanById(Guid id)
        {
            Guid? memberProfileId = null;
            if (!User.IsInRole("Admin") && !User.IsInRole("Trainer"))
            {
                memberProfileId = _currentUserService.MemberProfileId;
            }

            var result = await _nutritionPlanService.GetNutritionPlanByIdAsync(id, memberProfileId);
            if (!result.IsSuccess)
                return NotFound(result.Errors);

            return Ok(result.Value);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNutritionPlan(Guid id)
        {
            Guid? memberProfileId = null;
            if (!User.IsInRole("Admin") && !User.IsInRole("Trainer"))
            {
                memberProfileId = _currentUserService.MemberProfileId;
            }

            var result = await _nutritionPlanService.DeleteNutritionPlanAsync(id, memberProfileId);
            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            return NoContent();
        }
    }
}
