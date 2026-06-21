using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientAppointmentManagementServices.GetPatientProfilesForBookingServices
{
    /// <summary>
    /// Defines the operational abstraction boundary contract executing authorized patient profile list resolutions.
    /// </summary>
    public interface IGetPatientProfilesForBookingService
    {
        /// <summary>
        /// Resolves the internal data query workflow streams to parse and return un-paged patient profiles bound tightly onto active user identity contexts.
        /// </summary>
        /// <returns>An <see cref="ApiResponse{List{PatientProfileOption}}"/> enclosing descriptive state presentation payloads for selection layouts.</returns>
        Task<ApiResponse<List<PatientProfileOption>>> Process();
    }
}