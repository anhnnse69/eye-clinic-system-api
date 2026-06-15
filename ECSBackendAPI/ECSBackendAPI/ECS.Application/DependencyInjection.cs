using System.Reflection;
using ECS.Application.Services.AuthServices.ForgotPasswordServices;
using ECS.Application.Services.AuthServices.LoginServices;
using ECS.Application.Services.AuthServices.RegisterServices;
using ECS.Application.Services.AuthServices.ResetPasswordServices;
using ECS.Application.Services.AuthServices.ViewAccountInfoServices;
using ECS.Application.Services.AuthServices.ViewPersonalProfileServices;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicAppointmentServices;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicDashboardServices;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicFeedbackServices;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicProfileServices;
using ECS.Application.Services.ClinicAdminManagementServices.CreateStaffAccountServices;
using ECS.Application.Services.ClinicAdminManagementServices.DeleteClinicFeedbackServices;
using ECS.Application.Services.ClinicAdminManagementServices.EditClinicProfileServices;
using ECS.Application.Services.ClinicAdminManagementServices.EditStaffAccountServices;
using ECS.Application.Services.ClinicAdminManagementServices.ViewListStaffAccountsServices;
using ECS.Application.Services.ClinicDoctorDiscoveryService.RegisterClinicApplicationServices;
using ECS.Application.Services.ClinicDoctorDiscoveryService.SearchClinicDoctorServices;
using ECS.Application.Services.PatientProfileManagementServices.GetPatientProfilesServices;
using ECS.Application.Services.ClinicDoctorDiscoveryService.ViewClinicProfileServices;
using ECS.Application.Services.ClinicDoctorDiscoveryService.ViewDoctorSlotsServices;
using ECS.Application.Services.SystemAdminServices.AdminSystemDeleteClinicServices;
using ECS.Application.Services.SystemAdminServices.AdminSystemGetClinicDetailsServices;
using ECS.Application.Services.SystemAdminServices.AdminSystemGetDashboardServices;
using ECS.Application.Services.SystemAdminServices.AdminSystemUpdateClinicServices;
using ECS.Application.Services.SystemAdminServices.ApproveClinicApplicationServices;
using ECS.Application.Services.SystemAdminServices.ClinicManagementServices;
using ECS.Application.Services.SystemAdminServices.ClinicRegisterServices;
using ECS.Application.Services.SystemAdminServices.RejectClinicApplicationServices;
using ECS.Application.Services.SystemAdminServices.ReviewClinicRegisterServices;
using ECS.Application.Services.ClinicAdminManagementServices.ViewListServiceServices;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<IViewAccountInfoService, ViewAccountInfoService>();
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
        services.AddScoped<IGetPatientProfilesService, GetPatientProfilesService>();
        services.AddScoped<IEditStaffService, EditStaffService>();
        services.AddScoped<IDeleteClinicFeedbackService, DeleteClinicFeedbackService>();
        services.AddScoped<IViewClinicProfileService, ViewClinicProfileService>();
        services.AddScoped<IAdminSystemGetDashboardService, AdminSystemGetDashboardService>();
        services.AddScoped<IGetPersonalProfileService, GetPersonalProfileService>();
        services.AddScoped<IViewDoctorSlotsService, ViewDoctorSlotsService>();
        services.AddScoped<IViewClinicServicesService, ViewClinicServicesService>();

        // ── FluentValidation ──────────────────────────────────
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // ── AutoMapper ────────────────────────────────────────
        // services.AddAutoMapper(Assembly.GetExecutingAssembly());

        return services;
    }
}
