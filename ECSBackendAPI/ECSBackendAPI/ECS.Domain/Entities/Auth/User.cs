using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Configurations;
using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Notifications;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.Auth
{
    public class User : EntityBase<Guid>
    {
        public string Phone { get; set; } = null!;
        public string? Email { get; set; }
        public string PasswordHash { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<PatientProfile>? PatientProfiles { get; set; }
        public virtual ICollection<DoctorProfile>? DoctorProfiles { get; set; }
        public virtual ICollection<UserPatient>? UserPatients { get; set; }
        public virtual ICollection<ClinicRegistrationRequest>? ReviewedRequests { get; set; }
        public virtual ICollection<StaffClinic>? StaffClinics { get; set; }
        public virtual ICollection<Appointment>? AppointmentsCreated { get; set; }
        public virtual ICollection<EmrExportLog>? EmrExportLogs { get; set; }
        public virtual ICollection<DocumentAccessPermission>? GrantedPermissions { get; set; }
        public virtual ICollection<DocumentAccessPermission>? GrantedByPermissions { get; set; }
        public virtual ICollection<Notification>? Notifications { get; set; }
        public virtual ICollection<AuditLog>? AuditLogs { get; set; }
    }
}