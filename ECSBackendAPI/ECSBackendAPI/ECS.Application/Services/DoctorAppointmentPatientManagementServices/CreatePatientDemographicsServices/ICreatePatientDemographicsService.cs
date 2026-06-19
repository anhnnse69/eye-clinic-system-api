using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.CreatePatientDemographicsServices
{
    /// <summary>
    /// Defines business transaction workflows for create patient demographics.
    /// </summary>
    public interface ICreatePatientDemographicsService
    {
        /// <summary>
        /// Executes orchestration process logic to create a new patient demographics record with optional medical record.
        /// </summary>
        /// <param name="request">The structural data parameter carrier specifying target demographics settings attributes.</param>
        /// <returns>An encapsulated standard workflow execution response tracking model wrapper.</returns>
        Task<ApiResponse<CreatePatientDemographicsResponse>> Process(CreatePatientDemographicsRequest request);
    }
}
