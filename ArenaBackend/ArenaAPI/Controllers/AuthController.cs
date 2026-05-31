using ArenaApplication.Dtos.AuthDtos;
using ArenaApplication.Dtos.loginDto;
using ArenaApplication.Dtos.RegisterDto;
using ArenaApplication.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArenaApi.Controllers
{

    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(result.Errors);
            return CreatedAtAction(nameof(GetProfile), result.Value);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserloginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (!result.IsSuccess)
                return Unauthorized(result.Errors);
            return Ok(result.Value);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenDto dto)
        {
            var result = await _authService.RefreshTokenAsync(dto);
            if (!result.IsSuccess)
                return Unauthorized(result.Errors);
            return Ok(result.Value);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _authService.LogoutAsync(userId);
            if (!result.IsSuccess)
                return BadRequest(result.Errors);
            return NoContent();
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _authService.GetProfileAsync(userId);
            if (!result.IsSuccess)
                return NotFound(result.Errors);
            return Ok(result.Value);
        }

        [Authorize]
        [HttpPatch("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _authService.ChangePasswordAsync(userId, dto);
            if (!result.IsSuccess)
                return BadRequest(result.Errors);
            return NoContent();
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            var result = await _authService.ForgotPasswordAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(result.Errors);
            return Ok("If the email exists a reset link has been sent");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var result = await _authService.ResetPasswordAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(result.Errors);
            return NoContent();
        }
    }
}
