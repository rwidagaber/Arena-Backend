using ArenaApplication.Dtos.Payment;
using ArenaApplication.IServices.Payment;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArenaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        private Guid GetCurrentUserId()
           => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        //POST api/payments
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePaymentDto dto)
        {
            var result = await _paymentService.CreateAsync(dto, GetCurrentUserId());

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Errors });

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Value!.Id },
                result.Value);
        }
    }
}
