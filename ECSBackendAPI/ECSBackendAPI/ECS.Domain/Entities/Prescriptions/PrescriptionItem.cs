using ECS.Domain.Entities.General;

namespace ECS.Domain.Entities.Prescriptions
{
    /// <summary>
    /// Individual item in a prescription.
    /// </summary>
    public class PrescriptionItem : EntityBase<Guid>
    {
        public Guid PrescriptionId { get; set; }
        public string MedicineName { get; set; } = null!;
        public string Dosage { get; set; } = null!;
        public string? Frequency { get; set; }
        public int? DurationDays { get; set; }
        public int Quantity { get; set; }
        public string? Instruction { get; set; }

        public virtual Prescription Prescription { get; set; } = null!;
    }
}
