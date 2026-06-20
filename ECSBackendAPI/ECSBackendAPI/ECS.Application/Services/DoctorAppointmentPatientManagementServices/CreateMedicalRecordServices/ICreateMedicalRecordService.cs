using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.CreateMedicalRecordServices
{
    /// <summary>
    /// Interface for CreateMedicalRecordService.
    /// </summary>
    public interface ICreateMedicalRecordService
    {
        /// <summary>
        /// Processes the medical record creation request.
        /// </summary>
        /// <param name="request">The create medical record request.</param>
        /// <returns>An ApiResponse containing the created medical record details.</returns>
        Task<ApiResponse<CreateMedicalRecordResponse>> Process(CreateMedicalRecordRequest request);
    }
}
