using ECS.Infrastructure.ConfigService.JwtService;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database ─────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
            )
            .UseSnakeCaseNamingConvention());

        // ── Unit of Work ──────────────────────────────────────
        services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));

        // ── Repositories ───────────────────────────────────────
        services.AddScoped(typeof(IRepositoryQueryBase<,,>), typeof(RepositoryQueryBase<,,>));
        services.AddScoped(typeof(IRepositoryBaseAsync<,,>), typeof(RepositoryBase<,,>));

        // ── Services ──────────────────────────────────────────
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}