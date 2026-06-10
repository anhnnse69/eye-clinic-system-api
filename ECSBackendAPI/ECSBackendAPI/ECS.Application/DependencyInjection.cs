using ECS.Application.Services.AuthServices.LoginServices;
using ECS.Application.Services.AuthServices.RegisterServices;
using ECS.Application.Services.SystemAdminServices.ClinicRegisterServices;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ECS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // ── MediatR ───────────────────────────────────────────
        // Registers all IRequestHandler, INotificationHandler in this assembly.
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<IRegisterService, RegisterService>();
        services.AddScoped<IGetClinicApplicationService, GetClinicApplicationService>();


        // ── FluentValidation ──────────────────────────────────
        // Registers all AbstractValidator<T> in this assembly.
        // services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // ── AutoMapper ────────────────────────────────────────
        // services.AddAutoMapper(Assembly.GetExecutingAssembly());

        return services;
    }
}