using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Interfaces
{
    public interface IGenericRepository<TEntity, TId> where TEntity : BaseEntity<TId>
    {
        public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        public Task SoftDeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

        public Task HardDeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

        public IQueryable<TEntity> GetAll();
        public Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
