using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientAppointmentManagementServices.GetClinicServicesForBookingServices
{
    /// <summary>
    /// Defines the operational abstraction boundary contract executing healthcare service list resolutions within specified clinics.
    /// </summary>
    public interface IGetClinicServicesForBookingService
    {
        /// <summary>
        /// Resolves the internal data query workflow streams to parse and return services bound tightly onto a specific clinic identifier context.
        /// </summary>
        /// <param name="clinicId">The unique identifier mapping the targeted underlying clinic entity.</param>
        /// <returns>An <see cref="ApiResponse{List{BookingServiceOption}}"/> enclosing descriptive state presentation payloads for selection layouts.</returns>
        Task<ApiResponse<List<BookingServiceOption>>> Process(Guid clinicId);
    }
}