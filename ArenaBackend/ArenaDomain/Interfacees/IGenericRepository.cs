using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace ArenaDomain.Interfacees
{
    public interface IGenericRepository<TEntity, TId> where TEntity : BaseEntity<TId>
    {
        public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        public Task SoftDeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

        public Task HardDeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

        public IQueryable<TEntity> GetAll();
        public Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);


      public Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

        public Task<List<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default);
    
}
}
