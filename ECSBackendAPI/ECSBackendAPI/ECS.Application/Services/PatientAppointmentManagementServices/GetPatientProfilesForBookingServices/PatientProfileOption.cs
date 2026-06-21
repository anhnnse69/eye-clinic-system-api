namespace ECS.Application.Services.PatientAppointmentManagementServices.GetPatientProfilesForBookingServices
{
    /// <summary>
    /// Represents a decoupled presented serialization data schema for patient profile options in selection boundaries.
    /// </summary>
    public class PatientProfileOption
    {
        /// <summary>
        /// Gets or sets the unique primary key reference tracking identifier value.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the formal complete identity descriptor tracking moniker sequence.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the evaluated biological classification coordinate text representation.
        /// </summary>
        public string Gender { get; set; } = null!;

        /// <summary>
        /// Gets or sets the chronological calendar coordinate string mapping formatted dates of birth.
        /// </summary>
        public string Dob { get; set; } = null!;

        /// <summary>
        /// Gets or sets the interpersonal metadata definition structural link tracking connections.
        /// </summary>
        public string Relationship { get; set; } = null!;
    }
}