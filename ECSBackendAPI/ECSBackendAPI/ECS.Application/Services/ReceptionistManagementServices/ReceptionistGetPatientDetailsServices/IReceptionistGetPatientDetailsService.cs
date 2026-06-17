using ECS.Application.Common.Response;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetPatientDetailsServices
{
    /// <summary>
    /// Interface for the service handling patient detailed profiles and localized appointment histories extraction.
    /// </summary>
    public interface IReceptionistGetPatientDetailsService
    {
        /// <summary>
        /// Resolves patient details and extracts a localized slice of operational appointment histories.
        /// </summary>
        /// <param name="request">The parameters package detailing consumer context and targeted profile boundaries.</param>
        /// <returns>An <see cref="ApiResponse{ReceptionistGetPatientDetailsResponse}"/> wrapping the filtered domain information.</returns>
        Task<ApiResponse<ReceptionistGetPatientDetailsResponse>> Process(ReceptionistGetPatientDetailsRequest request);
    }
}