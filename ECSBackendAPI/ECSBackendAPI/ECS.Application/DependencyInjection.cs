using System.Reflection;
using ECS.Application.Services.AuthServices.LoginServices;
using ECS.Application.Services.AuthServices.RegisterServices;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicProfileServices;
using ECS.Application.Services.ClinicAdminManagementServices.EditClinicProfileServices;
using ECS.Application.Services.SystemAdminServices.ApproveClinicApplicationServices;
using ECS.Application.Services.SystemAdminServices.ClinicRegisterServices;
using ECS.Application.Services.SystemAdminServices.RejectClinicApplicationServices;
using ECS.Application.Services.SystemAdminServices.ReviewClinicRegisterServices;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<IGetClinicApplicationDetailService, GetClinicApplicationDetailService>();
        services.AddScoped<IApproveClinicApplicationService, ApproveClinicApplicationService>();
        services.AddScoped<IRejectClinicApplicationService, RejectClinicApplicationService>();
        services.AddScoped<IViewClinicService, ViewClinicService>();
        services.AddScoped<IEditClinicProfileService, EditClinicProfileService>();


        // ── FluentValidation ──────────────────────────────────
        // Registers all AbstractValidator<T> in this assembly.
        // services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // ── AutoMapper ────────────────────────────────────────
        // services.AddAutoMapper(Assembly.GetExecutingAssembly());

        return services;
    }
}