using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientAppointmentManagementServices.ClinicSlotServices
{ /// <summary>
  /// Defines the operational abstraction boundary contract for retrieving available time slots within a clinic.
  /// </summary>
    public interface IClinicSlotService
    {
        /// <summary>
        /// Resolves the internal data query workflow streams to parse and return available time slots for a specific clinic.
        /// </summary>
        /// <param name="clinicId">The unique identifier mapping the targeted underlying clinic entity.</param>
        /// <param name="date">The date to check availability for (format: yyyy-MM-dd).</param>
        /// <param name="serviceId">Optional service identifier to filter slots.</param>
        /// <returns>An <see cref="ApiResponse{List{ClinicSlotResponse}}"/> enclosing descriptive state presentation payloads for selection layouts.</returns>
        Task<ApiResponse<List<ClinicSlotResponse>>> Process(Guid clinicId, string date, Guid? serviceId = null);
    }
}
