using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDetailServices
{
    /// <summary>
    /// Defines the contract for viewing patient details.
    /// </summary>
    public interface IViewPatientDetailService
    {
        /// <summary>
        /// Returns detailed information
        /// about a patient and their appointment history.
        /// </summary>
        Task<ApiResponse<ViewPatientDetailResponse>> Process(
            Guid doctorId,
            Guid patientId);
    }
}
