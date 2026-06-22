using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.DoctorDashboardServices
{
    /// <summary>
    /// Provides dashboard statistics and performance metrics for doctors.
    /// </summary>
    public interface IDoctorDashboardService
    {
        /// <summary>
        /// Retrieves dashboard data including schedule summary, appointment statistics,
        /// patient count, and appointment trends for the specified doctor.
        /// </summary>
        /// <param name="userId">
        /// Identifier of the authenticated doctor user.
        /// </param>
        /// <param name="request">
        /// Filter criteria containing the reporting date range.
        /// </param>
        /// <returns>
        /// A response containing dashboard statistics and trend data.
        /// </returns>
        Task<ApiResponse<DoctorDashboardResponse>> Process(
            Guid userId,
            DoctorDashboardRequest request);
    }
}
