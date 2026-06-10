using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.General;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;

namespace ECS.Domain.Entities.Feedbacks
{
    /// <summary>
    /// Patient feedback for appointment.
    /// </summary>
    public class Feedback : EntityBase<Guid>
    {
        public Guid AppointmentId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid ClinicId { get; set; }
        public int RatingDoctor { get; set; }
        public int RatingClinic { get; set; }
        public string? Comment { get; set; }
        public bool IsPublic { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Appointment Appointment { get; set; } = null!;
        public virtual PatientProfile Patient { get; set; } = null!;
        public virtual DoctorProfile Doctor { get; set; } = null!;
        public virtual Clinic Clinic { get; set; } = null!;
    }
}
