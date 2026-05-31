using ECS.Domain.Entities.General;

namespace ECS.Domain.Entities.Clinics
{
    public class Specialty : EntityBase<Guid>
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual ICollection<DoctorProfile>? DoctorProfiles { get; set; }
    }

}
