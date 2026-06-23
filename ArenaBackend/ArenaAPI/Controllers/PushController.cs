// ArenaApi/Controllers/PushController.cs
using ArenaApplication.Dtos.NotificationDtos;
using ArenaApplication.Dtos.NotificationDtos;
using ArenaDomain.Entities.Notifications;
using ArenaInfrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArenaApi.Controllers
{
    [ApiController]
    [Route("api/push")]
    [Authorize]
    public class PushController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PushController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var existing = _context.PushSubscriptions
                .FirstOrDefault(s => s.UserId == userId && s.Endpoint == dto.Endpoint);

            if (existing != null) return Ok();

            _context.PushSubscriptions.Add(new PushSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Endpoint = dto.Endpoint,
                P256dh = dto.Keys.P256dh,
                Auth = dto.Keys.Auth
            });

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] string endpoint)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var sub = _context.PushSubscriptions
                .FirstOrDefault(s => s.UserId == userId && s.Endpoint == endpoint);

            if (sub != null)
            {
                _context.PushSubscriptions.Remove(sub);
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }
    }
}