using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArenaDomain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}