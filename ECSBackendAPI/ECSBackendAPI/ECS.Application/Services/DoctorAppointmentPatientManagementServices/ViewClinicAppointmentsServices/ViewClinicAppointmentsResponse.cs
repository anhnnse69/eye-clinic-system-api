using ECS.Domain.Enums;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewClinicAppointmentsServices
{
    /// <summary>
    /// Represents a paginated response
    /// for a clinic-wide appointment list.
    /// </summary>
    public class ViewClinicAppointmentsResponse
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public List<ClinicAppointmentItem> Appointments { get; set; } = [];
    }

    /// <summary>
    /// Represents an appointment item in the clinic-wide appointment list.
    /// Extends the doctor-facing item with doctor identity fields since
    /// the receptionist views appointments across multiple doctors.
    /// </summary>
    public class ClinicAppointmentItem
    {
        public Guid AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = null!;
        public string? Symptoms { get; set; }
        public string BookingSource { get; set; } = null!;
        public decimal DepositAmount { get; set; }
        public bool DepositPaid { get; set; }

        // Doctor info
        public Guid DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public string? DoctorTitle { get; set; }
        public string? DoctorAvatarUrl { get; set; }

        // Patient info
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = null!;
        public string? PatientPhone { get; set; }
        public string? PatientAvatarUrl { get; set; }
        public Gender PatientGender { get; set; }
        public DateTime? PatientDob { get; set; }

        // Service info
        public Guid? ServiceId { get; set; }
        public string? ServiceName { get; set; }
        public decimal? ServicePrice { get; set; }

        // Slot info
        public DateTime SlotStartTime { get; set; }
        public DateTime SlotEndTime { get; set; }

        // Medical record
        public bool HasMedicalRecord { get; set; }
        public Guid? MedicalRecordId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}