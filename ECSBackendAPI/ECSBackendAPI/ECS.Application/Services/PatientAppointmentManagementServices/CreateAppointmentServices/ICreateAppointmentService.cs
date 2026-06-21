using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientAppointmentManagementServices.CreateAppointmentServices
{
    /// <summary>
    /// Defines the operational abstraction boundary contract executing authorized patient appointment creation workflows.
    /// </summary>
    public interface ICreateAppointmentService
    {
        /// <summary>
        /// Processes the internal transactional business logic data pipeline to validate parameters, check restrictions, and commit a new appointment record.
        /// </summary>
        /// <param name="request">The data container tracking transaction parameters and structural entity keys requested by the presentation layer.</param>
        /// <returns>An <see cref="ApiResponse{CreateAppointmentResponse}"/> enclosing descriptive state transaction payloads alongside system outcomes.</returns>
        Task<ApiResponse<CreateAppointmentResponse>> Process(CreateAppointmentRequest request);
    }
}