using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientProfileManagementServices.GetPatientProfilesServices
{
    /// <summary>
    /// Service contract defining operations for viewing patient profiles associated with the logged-in user.
    /// </summary>
    public interface IGetPatientProfilesService
    {
        /// <summary>
        /// Executes the application workflow process to retrieve and filter profiles owned by or linked to the active patient session.
        /// </summary>
        /// <param name="request">The view profiles request criteria parameter details.</param>
        /// <returns>A unified standard envelope tracking result execution response block.</returns>
        Task<ApiResponse<List<GetPatientProfileResponse>>> Process(GetPatientProfilesRequest request);
    }
}
