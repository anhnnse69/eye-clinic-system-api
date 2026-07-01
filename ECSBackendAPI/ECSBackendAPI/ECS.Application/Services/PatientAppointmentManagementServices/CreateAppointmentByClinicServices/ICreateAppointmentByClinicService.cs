using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientAppointmentManagementServices.CreateAppointmentByClinicServices
{
    /// <summary>
    /// Service contract defining operations for creating patient appointments based on clinic working hours without specific doctor selection.
    /// </summary>
    public interface ICreateAppointmentByClinicService
    {
        /// <summary>
        /// Executes the application workflow process to create an appointment by clinic working hours.
        /// </summary>
        /// <param name="request">The appointment creation request parameters.</param>
        /// <returns>A unified standard envelope tracking result execution response block.</returns>
        Task<ApiResponse<CreateAppointmentByClinicResponse>> Process(CreateAppointmentByClinicRequest request);
    }
}
