namespace ECS.Application.Services.PatientAppointmentManagementServices.GetClinicServicesForBookingServices
{
    /// <summary>
    /// Represents a decoupled presented serialization data schema for healthcare service options in selection boundaries.
    /// </summary>
    public class BookingServiceOption
    {
        /// <summary>
        /// Gets or sets the unique primary key reference tracking identifier value for the service.
        /// </summary>
        public Guid Id_service { get; set; }

        /// <summary>
        /// Gets or sets the formal complete entity descriptor tracking moniker sequence of the service.
        /// </summary>
        public string ServiceName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the economic financial parameter mapping the baseline cost metrics configuration safely.
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Gets or sets the quantified chronological temporal index tracking duration boundaries in minutes.
        /// </summary>
        public int DurationMinutes { get; set; }
    }
}