using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientAppointmentManagementServices.GetPrescriptionDetailServices
{
    /// <summary>
    /// Contract for UC 22: View Prescription details service.
    /// </summary>
    public interface IGetPrescriptionDetailService
    {
        /// <summary>
        /// Retrieves detailed prescription information for a specified appointment.
        /// </summary>
        /// <param name="request">The request parameters containing AppointmentId.</param>
        /// <returns>An ApiResponse wrapper containing prescription detail payload.</returns>
        Task<ApiResponse<GetPrescriptionDetailResponse>> Process(GetPrescriptionDetailRequest request);
    }
}
