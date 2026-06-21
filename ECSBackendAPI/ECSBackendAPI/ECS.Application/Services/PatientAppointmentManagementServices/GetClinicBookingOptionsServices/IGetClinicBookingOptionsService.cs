using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientAppointmentManagementServices.GetClinicBookingOptionsServices
{
    /// <summary>
    /// Defines the operational abstraction boundary contract executing doctor profile list resolutions within specified clinics.
    /// </summary>
    public interface IGetClinicDoctorsForBookingService
    {
        /// <summary>
        /// Resolves the internal data query workflow streams to parse and return doctors bound tightly onto a specific clinic identifier context.
        /// </summary>
        /// <param name="clinicId">The unique identifier mapping the targeted underlying clinic entity.</param>
        /// <returns>An <see cref="ApiResponse{List{BookingDoctorOption}}"/> enclosing descriptive state presentation payloads for selection layouts.</returns>
        Task<ApiResponse<List<BookingDoctorOption>>> Process(Guid clinicId);
    }
}