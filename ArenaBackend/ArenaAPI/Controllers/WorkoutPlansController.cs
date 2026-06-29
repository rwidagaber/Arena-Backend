using ArenaApplication.IServices;
using ArenaApi.Configurations.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ArenaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [TypeFilter(typeof(RequireAIPlanFilter))]
    public class WorkoutPlansController : ControllerBase
    {
        private readonly IWorkoutPlanService _workoutPlanService;
        private readonly ICurrentUserService _currentUserService;

        public WorkoutPlansController(IWorkoutPlanService workoutPlanService, ICurrentUserService currentUserService)
        {
            _workoutPlanService = workoutPlanService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyWorkoutPlans()
        {
            var memberProfileId = _currentUserService.MemberProfileId;
            var result = await _workoutPlanService.GetWorkoutPlansByMemberIdAsync(memberProfileId);
            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            return Ok(result.Value);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetMyActiveWorkoutPlan()
        {
            var memberProfileId = _currentUserService.MemberProfileId;
            var result = await _workoutPlanService.GetActiveWorkoutPlanByMemberIdAsync(memberProfileId);
            if (!result.IsSuccess)
                return NotFound(result.Errors);

            return Ok(result.Value);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetWorkoutPlanById(Guid id)
        {
            Guid? memberProfileId = null;
            if (!User.IsInRole("Admin") && !User.IsInRole("Trainer"))
            {
                memberProfileId = _currentUserService.MemberProfileId;
            }

            var result = await _workoutPlanService.GetWorkoutPlanByIdAsync(id, memberProfileId);
            if (!result.IsSuccess)
                return NotFound(result.Errors);

            return Ok(result.Value);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkoutPlan(Guid id)
        {
            Guid? memberProfileId = null;
            if (!User.IsInRole("Admin") && !User.IsInRole("Trainer"))
            {
                memberProfileId = _currentUserService.MemberProfileId;
            }

            var result = await _workoutPlanService.DeleteWorkoutPlanAsync(id, memberProfileId);
            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            return NoContent();
        }
    }
}
