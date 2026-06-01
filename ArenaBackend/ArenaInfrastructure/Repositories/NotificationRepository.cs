using ArenaDomain.Entities.Notifications;
using ArenaDomain.Interfaces;
using ArenaInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArenaInfrastructure.Repositories
{
    public class NotificationRepository : GenericRepository<Notification, Guid>, INotificationRepository
    {
        private readonly AppDbContext _context;

        public NotificationRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
               .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted, cancellationToken);
        }

        public async Task<List<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
              .Where(n => n.UserId == userId && !n.IsDeleted)
              .OrderByDescending(n => n.CreatedAt)
              .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                 .CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDeleted, cancellationToken);
        }

        public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            await _context.Notifications
        .Where(n => n.UserId == userId && !n.IsDeleted && !n.IsRead)
        .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);
        }
    }
}
