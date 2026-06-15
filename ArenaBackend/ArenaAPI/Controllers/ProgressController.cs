using ArenaApplication.Dtos.ProgressLogDtos;
using ArenaApplication.IServices.IProgressServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArenaApi.Controllers
{

    [ApiController]
    [Route("api/progress")]
    [Authorize]
    public class ProgressController : ControllerBase
    {
        private readonly IProgressService _progressService;

        public ProgressController(IProgressService progressService)
        {
            _progressService = progressService;
        }

        [HttpPost]
        public async Task<IActionResult> LogProgress(CreateProgressLogDto dto)
        {
            // Debug — see all claims
            var claims = User.Claims.Select(c => new { c.Type, c.Value });

            var memberProfileIdStr = User.FindFirstValue("memberProfileId");

            // Return debug info
            if (string.IsNullOrEmpty(memberProfileIdStr))
                return Unauthorized(new
                {
                    message = "memberProfileId claim is empty",
                    allClaims = claims
                });

            if (!Guid.TryParse(memberProfileIdStr, out var memberProfileId))
                return Unauthorized(new
                {
                    message = "memberProfileId is not a valid Guid",
                    value = memberProfileIdStr
                });

            var result = await _progressService.LogProgressAsync(memberProfileId, dto);

            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            return CreatedAtAction(nameof(GetProgress), result.Value);
        }

        [HttpGet]
        public async Task<IActionResult> GetProgress()
        {
            var memberProfileIdStr = User.FindFirstValue("memberProfileId");

            if (!Guid.TryParse(memberProfileIdStr, out var memberProfileId))
                return Unauthorized("Invalid member profile ID");

            var result = await _progressService.GetProgressAsync(memberProfileId);
            if (!result.IsSuccess)
                return NotFound(result.Errors);

            return Ok(result.Value);
        }

        [HttpDelete("{logId}")]
        public async Task<IActionResult> DeleteLog(Guid logId)
        {
            var memberProfileIdStr = User.FindFirstValue("memberProfileId");

            if (!Guid.TryParse(memberProfileIdStr, out var memberProfileId))
                return Unauthorized("Invalid member profile ID");

            var result = await _progressService.DeleteLogAsync(memberProfileId, logId);

            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            return NoContent();
        }

    }
}


