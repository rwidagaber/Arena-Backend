using ArenaApplication.Dtos.AuthDtos;
using ArenaApplication.Dtos.AuthDtos.loginDto;
using ArenaApplication.Dtos.RegisterDto;
using ArenaApplication.IServices;
using ArenaDomain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;


namespace ArenaApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public AuthController(IAuthService authService, IStringLocalizer<ArenaLocalization> localizer)
        {
            _authService = authService;
            _localizer = localizer;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            return Ok(new { userId = result.Value }); // ← رجّع userId
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailDto dto)
        {
            var result = await _authService.ConfirmEmailAsync(dto);

            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            return Ok(result.Value);
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserloginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (!result.IsSuccess)
            {
                if (result.Errors?.Contains("GOOGLE_ACCOUNT_ONLY") == true)
                    return Unauthorized(new { code = "GOOGLE_ACCOUNT_ONLY" });

                return Unauthorized(result.Errors);
            }
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
            return result.IsSuccess ? Ok() : BadRequest(result.Errors);
        }

        
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var result = await _authService.ResetPasswordAsync(dto);
            return result.IsSuccess ? Ok() : BadRequest(result.Errors);
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginDto dto)
        {
            var result = await _authService.GoogleLoginAsync(dto.IdToken);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
        }

        [Authorize]
        [HttpPost("complete-profile")]
        public async Task<IActionResult> CompleteProfile(CompleteProfileDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _authService.CompleteProfileAsync(userId, dto);
            return result.IsSuccess ? Ok() : BadRequest(result.Errors);
        }

        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationDto dto)
        {
            var result = await _authService.ResendConfirmationAsync(dto.UserId);
            return result.IsSuccess ? Ok() : BadRequest(result.Errors);
        }
    }
}
