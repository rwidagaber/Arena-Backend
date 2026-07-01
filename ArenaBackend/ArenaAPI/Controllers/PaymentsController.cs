using ArenaApplication.Dtos.Payment;
using ArenaApplication.IServices;
using ArenaApplication.IServices.Payment;
using ArenaDomain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using System.Security.Cryptography;

namespace ArenaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IPaymentGatewayService _gatewayService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAnalyticsCacheVersionService _analyticsCacheVersionService;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;
        private readonly IConfiguration _config;

        public PaymentsController(IPaymentService paymentService,
                IPaymentGatewayService gatewayService,
                ICurrentUserService currentUserService,
                IAnalyticsCacheVersionService analyticsCacheVersionService,
                IStringLocalizer<ArenaLocalization> localizer,
                IConfiguration config)
        {
            _paymentService = paymentService;
            _gatewayService = gatewayService;
            _currentUserService = currentUserService;
            _analyticsCacheVersionService = analyticsCacheVersionService;
            _localizer = localizer;
            _config = config;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreatePaymentDto dto)
        {
            var userId = _currentUserService.UserId;
            var result = await _paymentService.CreateAsync(dto, userId);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Errors });

            _analyticsCacheVersionService.BumpVersion();

            return Ok(result.Value);
        }

        [HttpGet("my-payments")]
        [Authorize]
        public async Task<IActionResult> GetMyPayments()
        {
            var userId = _currentUserService.UserId;
            var result = await _paymentService.GetMyPaymentsAsync(userId);
            return Ok(result.Value);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _paymentService.GetByIdAsync(id);

            if (!result.IsSuccess)
            {
                return NotFound(new { message = result.Errors });
            }
            return Ok(result.Value);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll([FromQuery] PaymentFilterDto dto)
        {
            var result = await _paymentService.GetAllAsync(dto);
            return Ok(result.Value);
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdatePaymentStatusDto dto)
        {
            var result = await _paymentService.UpdateStatusAsync(id, dto);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Errors });
            }

            _analyticsCacheVersionService.BumpVersion();

            return Ok(result.Value);
        }

        [HttpPost("webhook/completed")]
        [AllowAnonymous]
        public async Task<IActionResult> WebhookCompleted([FromBody] PaymobWebhookDto dto,
                                                          [FromQuery] string hmac)
        {
            if (!_gatewayService.VerifyWebhookHmac(dto, hmac))
                return Unauthorized(new { message = _localizer["InvalidWebhookSignature"] });

            var transactionId = dto.Obj.Id.ToString();
            var paymentIntentId = dto.Obj.Order.Id.ToString();

            if (dto.Obj.Success)
            {
                var result = await _paymentService.MarkAsCompletedAsync(
                    transactionId, paymentIntentId);

                if (!result.IsSuccess)
                    return BadRequest(new { message = result.Errors });

                _analyticsCacheVersionService.BumpVersion();
            }
            else
            {
                var result = await _paymentService.MarkAsFailedAsync(
                    paymentIntentId, dto.Obj.Data.Message);

                if (!result.IsSuccess)
                    return BadRequest(new { message = result.Errors });

                _analyticsCacheVersionService.BumpVersion();
            }

            return Ok();
        }

        // Getway payment Callback (Browser Redirect)
        [HttpGet("callback")]
        [AllowAnonymous]
        public IActionResult Callback([FromQuery] bool success)
        {
            var frontendUrl = _config["EmailSettings:FrontendUrl"] ?? "http://localhost:4200";
            var frontendHomeUrl = GetFrontendHomeUrl(frontendUrl);
            var successValue = success.ToString().ToLowerInvariant();

            return Redirect($"{frontendHomeUrl}/checkout?success={successValue}");
        }

        private static string GetFrontendHomeUrl(string frontendUrl)
        {
            var trimmedUrl = frontendUrl.TrimEnd('/');

            if (Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var uri))
            {
                return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
            }

            return trimmedUrl;
        }
    }
}
