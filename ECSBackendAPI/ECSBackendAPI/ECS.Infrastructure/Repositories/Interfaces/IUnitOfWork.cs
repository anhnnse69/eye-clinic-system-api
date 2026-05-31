using Microsoft.EntityFrameworkCore;

namespace ECS.Infrastructure.Repositories.Interfaces;

public interface IUnitOfWork<TContext> : IDisposable
    where TContext : DbContext
{
    Task<int> CommitAsync();
}
