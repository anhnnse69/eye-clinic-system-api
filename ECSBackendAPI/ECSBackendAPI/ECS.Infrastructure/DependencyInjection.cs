using ECS.Infrastructure.Ai;
using ECS.Infrastructure.CloudStorage;
using ECS.Infrastructure.ConfigService.EmailService;
using ECS.Infrastructure.ConfigService.JwtService;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Persistence.MongoDb;
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
        services.AddScoped<IEmailService, EmailService>();

        // ── Cloud Storage (Cloudinary) ─────────────────────────
        services.Configure<CloudinaryOptions>(configuration.GetSection(CloudinaryOptions.SectionName));
        services.AddHttpClient();
        services.AddSingleton<ICloudStorageService, CloudinaryStorageService>();

        // ── MongoDB (medical-record JSON + paraclinical + AI suggestions) ──
        services.Configure<MongoDbOptions>(configuration.GetSection(MongoDbOptions.SectionName));
        services.AddSingleton<IMongoDbContext, MongoDbContext>();

        // ── AI Service (FastAPI VGG16 OCT classifier) ──────────────────
        services.Configure<AiServiceOptions>(configuration.GetSection(AiServiceOptions.SectionName));
        services.AddHttpClient<IAiServiceClient, AiServiceClient>((sp, client) =>
        {
            var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiServiceOptions>>().Value;
            client.BaseAddress = new Uri(opt.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(opt.PredictTimeoutSeconds);
        });

        return services;
    }
}