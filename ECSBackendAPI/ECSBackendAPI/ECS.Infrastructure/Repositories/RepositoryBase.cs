using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Domain.Entities.General;

namespace ECS.Infrastructure.Repositories;

public class RepositoryBase<T, K, TContext>
    : RepositoryQueryBase<T, K, TContext>,
        IRepositoryBaseAsync<T, K, TContext>
    where T : EntityBase<K>
    where K : notnull
    where TContext : DbContext
{
    private readonly TContext _dbContext;
    private readonly IUnitOfWork<TContext> _unitOfWork;

    public RepositoryBase(TContext dbContext, IUnitOfWork<TContext> unitOfWork)
        : base(dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public Task<IDbContextTransaction> BeginTransactionAsync() =>
        _dbContext.Database.BeginTransactionAsync();

    public async Task EndTransactionAsync()
    {
        await SaveChangesAsync();
        await _dbContext.Database.CommitTransactionAsync();
    }

    public Task RollbackTransactionAsync() => _dbContext.Database.RollbackTransactionAsync();

    public async Task<K> CreateAsync(T entity)
    {
        await _dbContext.Set<T>().AddAsync(entity);
        return entity.Id;
    }

    public async Task<IList<K>> CreateListAsync(IEnumerable<T> entities)
    {
        var list = entities.ToList();
        await _dbContext.Set<T>().AddRangeAsync(list);
        return entities.Select(x => x.Id).ToList();
    }

    public async Task UpdateAsync(T entity)
    {
        if (_dbContext.Entry(entity).State == EntityState.Unchanged)
            return;

        T? exist = await _dbContext.Set<T>().FindAsync(entity.Id);
        if (exist is null) return;

        _dbContext.Entry(exist).CurrentValues.SetValues(entity);
    }

    public Task UpdateListAsync(IEnumerable<T> entities)
    {
        _dbContext.Set<T>().UpdateRange(entities);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity)
    {
        _dbContext.Set<T>().Remove(entity);
        return Task.CompletedTask;
    }

    public Task DeleteListAsync(IEnumerable<T> entities)
    {
        _dbContext.Set<T>().RemoveRange(entities);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync() => _unitOfWork.CommitAsync();
}
