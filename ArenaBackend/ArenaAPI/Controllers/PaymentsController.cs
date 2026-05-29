using ArenaApplication.Dtos.Payment;
using ArenaApplication.IServices.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArenaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        //private Guid GetCurrentUserId()
        //   => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        //POST api/payments
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePaymentDto dto, [FromQuery] Guid userId)
        {
            var result = await _paymentService.CreateAsync(dto, userId);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Errors });

            return Ok(result.Value);
        }
        //get my payments
        [HttpGet("my-payments")]
        public async Task<IActionResult> GetMyPayments([FromQuery] Guid userId)
        {
            var result = await _paymentService.GetMyPaymentsAsync(userId);
            return Ok(result.Value);
        }
        //get payment by id
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _paymentService.GetByIdAsync(id);

            if(!result.IsSuccess)
            {
                return NotFound(new { message = result.Errors});
            }
            return Ok(result.Value);
        }
        //Admin
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaymentFilterDto dto)
        {
            var result = await _paymentService.GetAllAsync(dto);
            return Ok(result.Value);
        }

        //Admin change status
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id,[FromBody] UpdatePaymentStatusDto dto)
        {
            var result = await _paymentService.UpdateStatusAsync(id,dto);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Errors });
            }
            return Ok(result.Value);
        }
        //Getway payment Success
        [HttpPost("webhook/completed")]
        [AllowAnonymous]
        public async Task<IActionResult> WebhookCompleted([FromBody] WebhookDto dto)
        {
            var result = await _paymentService.MarkAsCompletedAsync(
                dto.TransactionId,
                dto.PaymentIntentId);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Errors });

            return Ok();
        }
        //Payment Faild
        [HttpPost("webhook/failed")]
        [AllowAnonymous]
        public async Task<IActionResult> WebhookFailed([FromBody] WebhookFailedDto dto)
        {
            var result = await _paymentService.MarkAsFailedAsync(
                dto.PaymentIntentId,
                dto.Reason);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Errors });

            return Ok();
        }
    }
}
