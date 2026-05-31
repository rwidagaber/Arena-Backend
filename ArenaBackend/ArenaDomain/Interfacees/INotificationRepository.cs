using ArenaDomain.Entities.Notifications;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ArenaDomain.Interfacees
{
    public interface INotificationRepository : IGenericRepository<Notification, Guid>
    {
        Task<List<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

        Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    }
}
