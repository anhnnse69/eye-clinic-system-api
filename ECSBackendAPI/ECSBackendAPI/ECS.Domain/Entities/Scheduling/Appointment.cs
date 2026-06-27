using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Feedbacks;
using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.Scheduling
{
    /// <summary>
    /// Patient appointment.
    /// </summary>
    public class Appointment : EntityBase<Guid>
    {
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid SlotId { get; set; }
        public Guid? ServiceId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string? Symptoms { get; set; }
        public AppointmentStatus Status { get; set; } = AppointmentStatus.PENDING;
        public decimal DepositAmount { get; set; } = 0;
        public bool DepositPaid { get; set; } = false;
        public string BookingSource { get; set; } = "ONLINE";
        public Guid? CreatedById { get; set; }
        public Guid? FollowUpFromAppointmentId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }
        public string? NoteReason { get; set; }

        public virtual PatientProfile Patient { get; set; } = null!;
        public virtual DoctorProfile Doctor { get; set; } = null!;
        public virtual TimeSlot Slot { get; set; } = null!;
        public virtual Service? Service { get; set; }
        public virtual User? CreatedBy { get; set; }
        public virtual Appointment? FollowUpFrom { get; set; }
        public virtual ICollection<Appointment>? FollowUpAppointments { get; set; }
        public virtual Queue? Queue { get; set; }
        public virtual MedicalRecord? MedicalRecord { get; set; }
        public virtual PreliminaryDiagnosis? PreliminaryDiagnosis { get; set; }
        public virtual Feedback? Feedback { get; set; }
    }
}
