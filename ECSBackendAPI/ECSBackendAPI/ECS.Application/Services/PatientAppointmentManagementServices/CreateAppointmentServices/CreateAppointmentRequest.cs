namespace ECS.Application.Services.PatientAppointmentManagementServices.CreateAppointmentServices
{
    /// <summary>
    /// Represents a decoupled transactional payload container holding initialization parameters requested by presentation layers to create a new appointment record.
    /// </summary>
    public class CreateAppointmentRequest
    {
        /// <summary>
        /// Gets or sets the unique primary key reference coordinates mapping the underlying patient profile core entity.
        /// </summary>
        public Guid PatientId { get; set; }

        /// <summary>
        /// Gets or sets the unique primary key reference coordinates mapping the underlying doctor profile core entity.
        /// </summary>
        public Guid DoctorId { get; set; }

        /// <summary>
        /// Gets or sets the unique primary key reference coordinates mapping the targeted timeline availability slot configuration.
        /// </summary>
        public Guid SlotId { get; set; }

        /// <summary>
        /// Gets or sets the optional unique primary key reference coordinates mapping the requested healthcare service entity.
        /// </summary>
        public Guid? ServiceId { get; set; }

        /// <summary>
        /// Gets or sets the explicit contextual medical textual notes tracking active physical issues reported directly out of presentation boundaries.
        /// </summary>
        public string? Symptoms { get; set; }
    }
}