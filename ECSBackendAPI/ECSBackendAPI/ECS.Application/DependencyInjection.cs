using ECS.Application.Services.AuthServices.ForgotPasswordServices;
using ECS.Application.Services.AuthServices.LoginServices;
using ECS.Application.Services.AuthServices.RegisterServices;
using ECS.Application.Services.AuthServices.ResetPasswordServices;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicAppointmentServices;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicDashboardServices;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicFeedbackServices;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicProfileServices;
using ECS.Application.Services.ClinicAdminManagementServices.CreateStaffAccountServices;
using ECS.Application.Services.ClinicAdminManagementServices.EditClinicProfileServices;
using ECS.Application.Services.ClinicAdminManagementServices.ViewListStaffAccountsServices;
using ECS.Application.Services.ClinicDoctorDiscoveryService.SearchClinicDoctorServices;
using ECS.Application.Services.ClinicDoctorDiscoveryService.RegisterClinicApplicationServices;
using ECS.Application.Services.SystemAdminServices.AdminSystemDeleteClinicServices;
using ECS.Application.Services.SystemAdminServices.AdminSystemGetClinicDetailsServices;
using ECS.Application.Services.SystemAdminServices.AdminSystemUpdateClinicServices;
using ECS.Application.Services.SystemAdminServices.ApproveClinicApplicationServices;
using ECS.Application.Services.SystemAdminServices.ClinicManagementServices;
using ECS.Application.Services.SystemAdminServices.ClinicRegisterServices;
using ECS.Application.Services.SystemAdminServices.RejectClinicApplicationServices;
using ECS.Application.Services.SystemAdminServices.ReviewClinicRegisterServices;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using ECS.Application.Services.ClinicAdminManagementServices.DeleteClinicFeedbackServices;

namespace ECS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // ── MediatR ───────────────────────────────────────────
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<IRegisterService, RegisterService>();
        services.AddScoped<IForgotPasswordService, ForgotPasswordService>();
        services.AddScoped<IResetPasswordService, ResetPasswordService>();
        services.AddScoped<IGetClinicApplicationService, GetClinicApplicationService>();
        services.AddScoped<IGetClinicApplicationDetailService, GetClinicApplicationDetailService>();
        services.AddScoped<IApproveClinicApplicationService, ApproveClinicApplicationService>();
        services.AddScoped<IRejectClinicApplicationService, RejectClinicApplicationService>();
        services.AddScoped<IViewClinicService, ViewClinicService>();
        services.AddScoped<IEditClinicProfileService, EditClinicProfileService>();
        services.AddScoped<IViewClinicDashboardService, ViewClinicDashboardService>();
        services.AddScoped<IViewListStaffService, ViewListStaffService>();
        services.AddScoped<IGetClinicsService, GetClinicsService>();
        services.AddScoped<ICreateStaffService, CreateStaffService>();
        services.AddScoped<IGetClinicFeedbacksService, GetClinicFeedbacksService>();
        services.AddScoped<IGetClinicAppointmentsService, GetClinicAppointmentsService>();
        services.AddScoped<IRegisterClinicApplicationService, RegisterClinicApplicationService>();
        services.AddScoped<ISearchClinicDoctorService, SearchClinicDoctorService>();
        services.AddScoped<IUpdateClinicService, UpdateClinicService>();
        services.AddScoped<IGetClinicDetailsService, GetClinicDetailsService>();
        services.AddScoped<IAdminSystemDeleteClinicService, AdminSystemDeleteClinicService>();
        services.AddScoped<IDeleteClinicFeedbackService, DeleteClinicFeedbackService>();

        // ── FluentValidation ──────────────────────────────────
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // ── AutoMapper ────────────────────────────────────────
        // services.AddAutoMapper(Assembly.GetExecutingAssembly());

        return services;
    }
}
