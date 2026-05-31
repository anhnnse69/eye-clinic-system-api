using Microsoft.EntityFrameworkCore;
using ECS.Infrastructure.Repositories.Interfaces;

namespace ECS.Infrastructure.Repositories;

public class UnitOfWork<TContext> : IUnitOfWork<TContext>
    where TContext : DbContext
{
    private readonly TContext _context;

    public UnitOfWork(TContext context)
    {
        _context = context;
    }

    public void Dispose() => _context.Dispose();

    public async Task<int> CommitAsync() => await _context.SaveChangesAsync();
}
