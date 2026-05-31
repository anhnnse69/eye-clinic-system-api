using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ECS.Domain.Entities.General;

namespace ECS.Infrastructure.Repositories.Interfaces;

public interface IRepositoryQueryBase<T, K, TContext>
    where T : EntityBase<K>
    where TContext : DbContext
{
    IQueryable<T> FindAll(bool trackChanges = false);
    IQueryable<T> FindAll(
        bool trackChanges = false,
        params Expression<Func<T, object>>[] includeProperties
    );
    IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false);

    IQueryable<T> FindByCondition(
        Expression<Func<T, bool>> expression,
        bool trackChanges = false,
        params Expression<Func<T, object>>[] includeProperties
    );

    Task<T?> GetByIdAsync(K id);
    Task<T?> GetByIdAsync(K id, params Expression<Func<T, object>>[] includeProperties);
}
