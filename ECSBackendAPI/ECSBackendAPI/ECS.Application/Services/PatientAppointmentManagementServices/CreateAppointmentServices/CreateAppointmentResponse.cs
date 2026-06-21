namespace ECS.Application.Services.PatientAppointmentManagementServices.CreateAppointmentServices
{
    /// <summary>
    /// Represents a decoupled presented serialization data schema for completed appointment transactions matching the presentation layer expectations.
    /// </summary>
    public class CreateAppointmentResponse
    {
        /// <summary>
        /// Gets or sets the unique primary key reference tracking identifier value for the created appointment.
        /// </summary>
        public Guid Id_appointment { get; set; }

        /// <summary>
        /// Gets or sets the formal complete identity descriptor tracking moniker sequence of the assigned doctor.
        /// </summary>
        public string DoctorName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the formal complete entity descriptor tracking moniker sequence of the targeted clinic.
        /// </summary>
        public string ClinicName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the formal complete entity descriptor tracking moniker sequence of the selected healthcare service.
        /// </summary>
        public string ServiceName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the chronological calendar coordinate string mapping formatted appointment execution dates in dd/MM/yyyy schema.
        /// </summary>
        public string AppointmentDate { get; set; } = null!;

        /// <summary>
        /// Gets or sets the chronological temporal segment index range description text string tracking slot boundaries in HH:mm - HH:mm schema.
        /// </summary>
        public string TimeSlot { get; set; } = null!;

        /// <summary>
        /// Gets or sets the evaluated workflow structural processing status coordinate representation text.
        /// </summary>
        public string Status { get; set; } = null!;

        /// <summary>
        /// Gets or sets the economic financial parameter mapping the mandatory advanced deposit cost metrics configuration safely.
        /// </summary>
        public decimal DepositAmount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the tracking transaction financial checkpoint indicator records complete deposit settlement.
        /// </summary>
        public bool DepositPaid { get; set; }

        /// <summary>
        /// Gets or sets the operational pipeline identifier source tracking the structural medium that dispatched the initial transaction.
        /// </summary>
        public string BookingSource { get; set; } = null!;
    }
}