namespace ECS.Application.Services.PatientAppointmentManagementServices.CreateAppointmentByClinicServices
{
    /// <summary>
    /// Response object containing comprehensive appointment details after successful creation by clinic.
    /// </summary>
    public class CreateAppointmentByClinicResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier of the created appointment.
        /// </summary>
        public Guid Id_appointment { get; set; }

        /// <summary>
        /// Gets or sets the full name of the assigned doctor.
        /// </summary>
        public string DoctorName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the name of the clinic.
        /// </summary>
        public string ClinicName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        public string ServiceName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the formatted appointment date.
        /// </summary>
        public string AppointmentDate { get; set; } = null!;

        /// <summary>
        /// Gets or sets the formatted time slot.
        /// </summary>
        public string TimeSlot { get; set; } = null!;

        /// <summary>
        /// Gets or sets the status of the appointment.
        /// </summary>
        public string Status { get; set; } = null!;

        /// <summary>
        /// Gets or sets the deposit amount for the appointment.
        /// </summary>
        public decimal DepositAmount { get; set; }

        /// <summary>
        /// Gets or sets whether the deposit has been paid.
        /// </summary>
        public bool DepositPaid { get; set; }

        /// <summary>
        /// Gets or sets the booking source of the appointment.
        /// </summary>
        public string BookingSource { get; set; } = null!;
    }
}
