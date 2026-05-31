using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ECS.Domain.Entities.General;

namespace ECS.Infrastructure.Repositories.Interfaces;

public interface IRepositoryBaseAsync<T, K, TContext> : IRepositoryQueryBase<T, K, TContext>
    where T : EntityBase<K>
    where TContext : DbContext
{
    Task<K> CreateAsync(T entity);
    Task<IList<K>> CreateListAsync(IEnumerable<T> entities);
    Task UpdateAsync(T entity);
    Task UpdateListAsync(IEnumerable<T> entities);
    Task DeleteAsync(T entity);
    Task DeleteListAsync(IEnumerable<T> entities);
    Task<int> SaveChangesAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task EndTransactionAsync();
    Task RollbackTransactionAsync();
}
