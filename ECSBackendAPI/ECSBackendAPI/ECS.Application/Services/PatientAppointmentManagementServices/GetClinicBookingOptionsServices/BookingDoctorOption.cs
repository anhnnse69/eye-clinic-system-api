namespace ECS.Application.Services.PatientAppointmentManagementServices.GetClinicBookingOptionsServices
{
    /// <summary>
    /// Represents a decoupled presented serialization data schema for doctor profile options in selection boundaries.
    /// </summary>
    public class BookingDoctorOption
    {
        /// <summary>
        /// Gets or sets the unique primary key reference tracking identifier value for the doctor.
        /// </summary>
        public Guid Id_doctor { get; set; }

        /// <summary>
        /// Gets or sets the formal complete identity descriptor tracking moniker sequence of the doctor.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the academic or medical designation label tracking structural professional ranks.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the formal domain operational area descriptor tracking core medical practices.
        /// </summary>
        public string? SpecialtyName { get; set; }

        /// <summary>
        /// Gets or sets the quantified chronological temporal index tracking aggregate professional work longevity.
        /// </summary>
        public int ExperienceYears { get; set; }
    }
}