using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Feedbacks;
using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.Patient
{
    /// <summary>
    /// Patient medical profile, can be linked to a User or stand-alone.
    /// </summary>
    public class PatientProfile : EntityBase<Guid>
    {
        public Guid? UserId { get; set; }
        public string FullName { get; set; } = null!;
        public Gender Gender { get; set; }
        public DateTime Dob { get; set; }
        public string? IdentityNumber { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? BhytNumber { get; set; }
        public string? BloodType { get; set; }
        public string? Allergies { get; set; }
        public string? MedicalHistory { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }

        public virtual User? User { get; set; }
        public virtual ICollection<UserPatient>? UserPatients { get; set; }
        public virtual ICollection<Appointment>? Appointments { get; set; }
        public virtual ICollection<MedicalRecord>? MedicalRecords { get; set; }
        public virtual ICollection<Feedback>? Feedbacks { get; set; }
    }
}
