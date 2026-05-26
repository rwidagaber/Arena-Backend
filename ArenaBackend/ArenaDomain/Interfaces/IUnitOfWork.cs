using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveChangesAsync();
    }
}
