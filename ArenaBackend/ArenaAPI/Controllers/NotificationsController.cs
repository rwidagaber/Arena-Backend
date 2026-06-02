using ArenaApplication.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArenaApi.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize(Roles = "GymMember")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUserService;

        public NotificationsController(
            INotificationService notificationService,
            ICurrentUserService currentUserService)
        {
            _notificationService = notificationService;
            _currentUserService = currentUserService;
        }

        // GET /api/notifications
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var notifications = await _notificationService.GetUserNotificationsAsync(
                _currentUserService.MemberProfileId, cancellationToken);

            return Ok(notifications);
        }

        // GET /api/notifications/unread-count
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
        {
            var count = await _notificationService.GetUnreadCountAsync(
                _currentUserService.MemberProfileId, cancellationToken);

            return Ok(new { unreadCount = count });
        }

        // PATCH /api/notifications/{id}/read
        [HttpPatch("{id:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
        {
            await _notificationService.MarkAsReadAsync(
                id, _currentUserService.MemberProfileId, cancellationToken);

            return NoContent();
        }

        // PATCH /api/notifications/read-all
        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
        {
            await _notificationService.MarkAllAsReadAsync(
                _currentUserService.MemberProfileId, cancellationToken);

            return NoContent();
        }
    }
}
