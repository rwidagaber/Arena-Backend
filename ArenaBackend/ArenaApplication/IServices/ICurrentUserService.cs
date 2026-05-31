using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.IServices
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
        string Email { get; }
    }
}
