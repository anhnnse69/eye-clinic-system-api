using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Feedbacks;
using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Scheduling;

namespace ECS.Domain.Entities.Clinics
{
    /// <summary>
    /// Doctor's profile inside a clinic.
    /// </summary>
    public class DoctorProfile : EntityBase<Guid>
    {
        public Guid UserId { get; set; }
        public Guid ClinicId { get; set; }
        public Guid? SpecialtyId { get; set; }
        public string? Title { get; set; }
        public int ExperienceYears { get; set; } = 0;
        public string? Bio { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal? RatingAvg { get; set; } = 0;
        public int? ReviewCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual Clinic Clinic { get; set; } = null!;
        public virtual Specialty? Specialty { get; set; }
        public virtual ICollection<DoctorSchedule>? DoctorSchedules { get; set; }
        public virtual ICollection<Appointment>? Appointments { get; set; }
        public virtual ICollection<MedicalRecord>? MedicalRecords { get; set; }
        public virtual ICollection<Feedback>? Feedbacks { get; set; }
    }
}
