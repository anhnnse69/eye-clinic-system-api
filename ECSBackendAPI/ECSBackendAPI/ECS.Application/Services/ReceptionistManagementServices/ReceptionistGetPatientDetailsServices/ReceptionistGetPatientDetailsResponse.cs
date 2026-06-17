namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetPatientDetailsServices
{
    /// <summary>
    /// Data transfer object defining comprehensive details for a specific patient file profile row layout.
    /// </summary>
    public class ReceptionistGetPatientDetailsResponse
    {
        public string Id { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Gender { get; set; } = null!; // "MALE", "FEMALE", "OTHER"
        public string Dob { get; set; } = null!; // "yyyy-MM-dd"
        public string? IdentityNumber { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? BhytNumber { get; set; }
        public string? BloodType { get; set; }
        public string? Allergies { get; set; }
        public string? MedicalHistory { get; set; }
        public string CreatedAt { get; set; } = null!; // "yyyy-MM-ddTHH:mm:ssZ"
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Strictly scoped log lists containing appointments booked only inside the receptionist's active clinic partition.
        /// </summary>
        public List<ReceptionistAppointmentLogDto> Appointments { get; set; } = new();
    }

    /// <summary>
    /// Data transfer object defining individual contextual operational appointment logging metrics elements.
    /// </summary>
    public class ReceptionistAppointmentLogDto
    {
        public string Id { get; set; } = null!;
        public string ClinicId { get; set; } = null!;
        public string AppointmentDate { get; set; } = null!; // "yyyy-MM-ddTHH:mm:ss"
        public string DoctorName { get; set; } = null!;
        public string SpecialtyName { get; set; } = null!;
        public string? Symptoms { get; set; }
        public string Status { get; set; } = null!; // "PENDING", "DEPOSIT_PAID", "BOOKED", "ARRIVED", "IN_PROGRESS", "COMPLETED", "CANCELLED", "NOSHOW"
        public string BookingSource { get; set; } = null!; // "MOBILE_APP", "WEBSITE", "WALK_IN"
    }
}