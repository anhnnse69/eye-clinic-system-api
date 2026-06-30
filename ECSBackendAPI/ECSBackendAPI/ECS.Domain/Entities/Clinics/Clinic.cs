using ECS.Domain.Entities.Feedbacks;
using ECS.Domain.Entities.General;
using ECS.Domain.Entities.Scheduling;

namespace ECS.Domain.Entities.Clinics
{
    /// <summary>
    /// A registered clinic.
    /// </summary>
    public class Clinic : EntityBase<Guid>
    {
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal? RatingAvg { get; set; } = 0;
        public int? ReviewCount { get; set; } = 0;
        public TimeOnly OpenTime { get; set; }
        public TimeOnly CloseTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }

        public virtual ICollection<DoctorProfile>? DoctorProfiles { get; set; }
        public virtual ICollection<Service>? Services { get; set; }
        public virtual ICollection<FacilityRoom>? FacilityRooms { get; set; }
        public virtual ICollection<StaffClinic>? StaffClinics { get; set; }
        public virtual ICollection<MedicineCatalog>? MedicineCatalogs { get; set; }
        public virtual ICollection<Queue>? Queues { get; set; }
        public virtual ICollection<Feedback>? Feedbacks { get; set; }
    }
}
