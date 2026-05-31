using ECS.Domain.Entities.General;

namespace ECS.Domain.Entities.Clinics
{
    public class MedicineCatalog : EntityBase<Guid>
    {
        public Guid ClinicId { get; set; }
        public string MedicineName { get; set; } = null!;
        public string? GenericName { get; set; }
        public string? Unit { get; set; }
        public string? DosageForm { get; set; }
        public string? Concentration { get; set; }
        public string? Manufacturer { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }

        public virtual Clinic Clinic { get; set; } = null!;
    }
}
