namespace ECS.Application.Services.PatientAppointmentManagementServices.ClinicSlotServices
{
    /// <summary>
    /// Represents a decoupled presented serialization data schema for clinic time slot options in selection boundaries.
    /// </summary>
    public class ClinicSlotResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier for the slot in format "clinic_{clinicId}_{date}_{startTime}".
        /// </summary>
        public string Id { get; set; } = null!;

        /// <summary>
        /// Gets or sets the start time of the slot (format: HH:mm).
        /// </summary>
        public string StartTime { get; set; } = null!;

        /// <summary>
        /// Gets or sets the end time of the slot (format: HH:mm).
        /// </summary>
        public string EndTime { get; set; } = null!;

        /// <summary>
        /// Gets or sets whether the slot is available for booking.
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Gets or sets the clinic identifier.
        /// </summary>
        public Guid ClinicId { get; set; }

        /// <summary>
        /// Gets or sets the date of the slot (format: yyyy-MM-dd).
        /// </summary>
        public string Date { get; set; } = null!;

        /// <summary>
        /// Gets or sets the doctor identifier if a doctor is available for this slot.
        /// </summary>
        public Guid? DoctorId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether there is an available doctor for this slot.
        /// </summary>
        public bool HasAvailableDoctor { get; set; }
    }
}