using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.IServices
{
    public interface INotificationHub
    {
      Task SendToUserAsync(Guid userId, object notification, CancellationToken cancellationToken = default);

    }
}
