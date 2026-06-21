using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.EyeExaminations
{
    /// <summary>
    /// Lacrimal system examination (MS22, MS26).
    /// </summary>
    public class LacrimalRecord : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public EyeSide Side { get; set; }

        // Lacrimal irrigation test
        public bool IrrigationFree { get; set; } = true;
        public bool IrrigationRegurgitationSame { get; set; } = false;
        public bool IrrigationRegurgitationOpposite { get; set; } = false;
        public string? IrrigationNote { get; set; }

        // Lacrimal other findings
        public string? LacrimalOther { get; set; }

        // Additional fields for MS26 (Pediatric)
        public string? LacrimalDischarge { get; set; }
        public string? NasolacrimalStatus { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
