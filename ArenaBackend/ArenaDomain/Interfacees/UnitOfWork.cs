using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArenaDomain.Interfacees
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}