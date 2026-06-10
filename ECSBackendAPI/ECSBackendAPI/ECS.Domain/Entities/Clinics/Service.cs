using ECS.Domain.Entities.General;
using ECS.Domain.Entities.Scheduling;

namespace ECS.Domain.Entities.Clinics
{
    /// <summary>
    /// Service offered by a clinic.
    /// </summary>
    public class Service : EntityBase<Guid>
    {
        public Guid ClinicId { get; set; }
        public string ServiceName { get; set; } = null!;
        public decimal? Price { get; set; }
        public int DurationMinutes { get; set; } = 15;
        public bool IsActive { get; set; } = true;

        public virtual Clinic Clinic { get; set; } = null!;
        public virtual ICollection<Appointment>? Appointments { get; set; }
    }
}
