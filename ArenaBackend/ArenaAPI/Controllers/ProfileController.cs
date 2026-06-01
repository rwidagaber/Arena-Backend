using ArenaApplication.Dtos.ProfileDtos;
using ArenaApplication.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArenaApi.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _profileService.GetProfileAsync(userId);
            if (!result.IsSuccess)
                return NotFound(result.Errors);

            return Ok(result.Value);
        }


        [HttpPut]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _profileService.UpdateProfileAsync(userId, dto);
            if (!result.IsSuccess)
                return BadRequest(result.Errors);
            return Ok("Profile Updated successfully");
        }
    }
}
