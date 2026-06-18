using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientProfileManagementServices.CreatePatientDemographicsServices
{
    /// <summary>
    /// Defines business transaction workflows for patient demographics creation.
    /// </summary>
    public interface ICreatePatientDemographicsService
    {
        /// <summary>
        /// Executes orchestration process logic to register a new patient demographics record.
        /// </summary>
        /// <param name="request">The structural data parameter carrier specifying target demographics settings attributes.</param>
        /// <returns>An encapsulated standard workflow execution response tracking model wrapper.</returns>
        Task<ApiResponse<CreatePatientDemographicsResponse>> Process(CreatePatientDemographicsRequest request);
    }
}
