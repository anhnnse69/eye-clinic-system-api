using ECS.Application.Services.AuthServices.ChangePasswordServices;
using ECS.Application.Services.AuthServices.ForgotPasswordServices;
using ECS.Application.Services.AuthServices.LoginServices;
using ECS.Application.Services.AuthServices.RegisterServices;
using ECS.Application.Services.AuthServices.ResetPasswordServices;
using ECS.Application.Services.AuthServices.UpdatePersonalProfileServices;
using ECS.Application.Services.AuthServices.ViewAccountInfoServices;
using ECS.Application.Services.AuthServices.ViewPersonalProfileServices;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicAppointmentServices;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicDashboardServices;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicFeedbackServices;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicProfileServices;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicRoomServices.CreateRoom;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicRoomServices.DeleteRoom;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicRoomServices.EditRoom;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicRoomServices.ViewListRoomServices;
using ECS.Application.Services.ClinicAdminManagementServices.CreateServiceServices;
using ECS.Application.Services.ClinicAdminManagementServices.CreateStaffAccountServices;
using ECS.Application.Services.ClinicAdminManagementServices.DeactivateServiceServices;
using ECS.Application.Services.ClinicAdminManagementServices.DeleteClinicFeedbackServices;
using ECS.Application.Services.ClinicAdminManagementServices.EditClinicProfileServices;
using ECS.Application.Services.ClinicAdminManagementServices.EditServiceServices;
using ECS.Application.Services.ClinicAdminManagementServices.EditStaffAccountServices;
using ECS.Application.Services.ClinicAdminManagementServices.MedicineCatalogServices.ViewList;
using ECS.Application.Services.ClinicAdminManagementServices.ViewListServiceServices;
using ECS.Application.Services.ClinicAdminManagementServices.ViewListStaffAccountsServices;
using ECS.Application.Services.ClinicDoctorDiscoveryService.GetActiveSpecialtiesServices;
using ECS.Application.Services.ClinicDoctorDiscoveryService.RegisterClinicApplicationServices;
using ECS.Application.Services.ClinicDoctorDiscoveryService.SearchClinicDoctorServices;
using ECS.Application.Services.ClinicDoctorDiscoveryService.ViewClinicFeedbacksServices;
using ECS.Application.Services.ClinicDoctorDiscoveryService.ViewClinicProfileServices;
using ECS.Application.Services.ClinicDoctorDiscoveryService.ViewDoctorSlotsServices;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.GetDetailPatientDemographicsServices;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewDoctorAppointmentsServices;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewListPatientServices;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDemographicsServices;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.CreatePatientDemographicsServices;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDetailServices;
using ECS.Application.Services.PatientProfileManagementServices.CreatePatientProfileServices;
using ECS.Application.Services.PatientProfileManagementServices.GetPatientProfilesServices;
using ECS.Application.Services.PatientProfileManagementServices.UpdatePatientProfileServices;
using ECS.Application.Services.PatientProfileManagementServices.ViewMyFeedbackHistoryServices;
using ECS.Application.Services.PatientProfileManagementServices.ViewPatientProfileDetailServices;
using ECS.Application.Services.ReceptionistManagementServices.ReceptionistCreatePatientProfileServices;
using ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetAvailableSlotsServices;
using ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetPatientDetailsServices;
using ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetPatientsListServices;
using ECS.Application.Services.ReceptionistManagementServices.ReceptionistSearchAccountServices;
using ECS.Application.Services.ReceptionistManagementServices.ReceptionistUpdatePatientProfileServices;
using ECS.Application.Services.SystemAdminServices.AdminSystemDeleteClinicServices;
using ECS.Application.Services.SystemAdminServices.AdminSystemGetClinicDetailsServices;
using ECS.Application.Services.SystemAdminServices.AdminSystemGetDashboardServices;
using ECS.Application.Services.SystemAdminServices.AdminSystemUpdateClinicServices;
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
        services.AddScoped<IViewClinicFeedbacksService, ViewClinicFeedbacksService>();
        services.AddScoped<IChangePasswordService, ChangePasswordService>();
        services.AddScoped<IViewListPatientService, ViewPatientListService>();
        services.AddScoped<IViewPatientDemographicsService, ViewPatientDemographicsService>();
        services.AddScoped<ICreateService, CreateService>();
        services.AddScoped<ICreatePatientProfileService, CreatePatientProfileService>();
        services.AddScoped<IUpdatePersonalProfileService, UpdatePersonalProfileService>();
        services.AddScoped<IGetActiveSpecialtiesService, GetActiveSpecialtiesService>();
        services.AddScoped<IEditServiceService, EditServiceService>();
        services.AddScoped<IViewPatientProfileDetailService, ViewPatientProfileDetailService>();
        services.AddScoped<IGetAvailableSlotsService, GetAvailableSlotsService>();
        services.AddScoped<IDeactivateService, DeactivateService>();
        services.AddScoped<IUpdatePatientProfileService, UpdatePatientProfileService>();
        services.AddScoped<IGetClinicRoomsService, GetClinicRoomsService>();
        services.AddScoped<IReceptionistGetPatientsListService, ReceptionistGetPatientsListService>();
        services.AddScoped<ICreatePatientDemographicsService, CreatePatientDemographicsService>();
        services.AddScoped<IReceptionistGetPatientDetailsService, ReceptionistGetPatientDetailsService>();
        services.AddScoped<IGetDetailPatientDemographicsService, GetDetailPatientDemographicsService>();
        services.AddScoped<IViewPatientDetailService, ViewPatientDetailService>();
        services.AddScoped<IReceptionistUpdatePatientProfileService, ReceptionistUpdatePatientProfileService>();
        services.AddScoped<IReceptionistCreatePatientProfileService, ReceptionistCreatePatientProfileService>();
        services.AddScoped<IReceptionistSearchAccountService, ReceptionistSearchAccountService>();

        services.AddScoped<IViewDoctorAppointmentsService, ViewDoctorAppointmentsService>();
        services.AddScoped<IDeleteRoomService, DeleteRoomService>();
        services.AddScoped<IViewMyFeedbackHistoryService, ViewMyFeedbackHistoryService>();
        services.AddScoped<IGetMedicineCatalogService, GetMedicineCatalogService>();
        services.AddScoped<ICreateRoomService, CreateRoomService>();
        services.AddScoped<IEditRoomService, EditRoomService>();
        // ── FluentValidation ──────────────────────────────────
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // ── AutoMapper ────────────────────────────────────────
        // services.AddAutoMapper(Assembly.GetExecutingAssembly());

        return services;
    }
}
