using ECS.Application.Services.AuthServices.LoginServices;
using ECS.Application.Services.AuthServices.RegisterClinicApplicationServices;
using ECS.Application.Services.AuthServices.RegisterServices;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicDashboardServices;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicProfileServices;
using ECS.Application.Services.ClinicAdminManagementServices.EditClinicProfileServices;
using ECS.Application.Services.ClinicAdminManagementServices.ViewListStaffAccountsServices;
using ECS.Application.Services.SystemAdminServices.ApproveClinicApplicationServices;
using ECS.Application.Services.SystemAdminServices.ClinicManagementServices;
using ECS.Application.Services.SystemAdminServices.ClinicRegisterServices;
using ECS.Application.Services.SystemAdminServices.RejectClinicApplicationServices;
using ECS.Application.Services.SystemAdminServices.ReviewClinicRegisterServices;
using FluentValidation;
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
        services.AddScoped<IGetClinicApplicationDetailService, GetClinicApplicationDetailService>();
        services.AddScoped<IApproveClinicApplicationService, ApproveClinicApplicationService>();
        services.AddScoped<IRejectClinicApplicationService, RejectClinicApplicationService>();
        services.AddScoped<IViewClinicService, ViewClinicService>();
        services.AddScoped<IEditClinicProfileService, EditClinicProfileService>();
        services.AddScoped<IViewClinicDashboardService, ViewClinicDashboardService>();
        services.AddScoped<IViewListStaffService, ViewListStaffService>();
        services.AddScoped<IGetClinicsService, GetClinicsService>();
        services.AddScoped<IRegisterClinicApplicationService, RegisterClinicApplicationService>();

        // ── FluentValidation ──────────────────────────────────
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // ── AutoMapper ────────────────────────────────────────
        // services.AddAutoMapper(Assembly.GetExecutingAssembly());

        return services;
    }
}