using ArenaDomain.Entities.Notifications;
using ArenaDomain.Interfaces;
using ArenaInfrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArenaInfrastructure.Repositories
{
    public class NotificationRepository : GenericRepository<Notification, Guid>, INotificationRepository
    {
        private readonly AppDbContext _context;

        public NotificationRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Notification>> GetByMemberProfileIdAsync(Guid memberProfileId, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .Where(n => n.MemberProfileId == memberProfileId && !n.IsDeleted)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(Guid memberProfileId, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .CountAsync(n => n.MemberProfileId == memberProfileId && !n.IsRead && !n.IsDeleted, cancellationToken);
        }

        public async Task MarkAllAsReadAsync(Guid memberProfileId, CancellationToken cancellationToken = default)
        {
            await _context.Notifications
                .Where(n => n.MemberProfileId == memberProfileId && !n.IsDeleted && !n.IsRead)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(n => n.IsRead, true),
                    cancellationToken);
        }

      
    }
}