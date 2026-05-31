using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;

namespace ECS.Domain.Entities.Paraclinical
{
    public class UltrasoundEye : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public string? UltrasoundType { get; set; }
        public string Side { get; set; } = null!;
        public decimal? AxialLengthMm { get; set; }
        public decimal? AnteriorChamberDepthMm { get; set; }
        public decimal? LensThicknessMm { get; set; }
        public decimal? VitreousLengthMm { get; set; }
        public string? LensStatus { get; set; }
        public string? VitreousStatus { get; set; }
        public string? RetinaStatus { get; set; }
        public string? Conclusion { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime ExamDate { get; set; }
        public string? TechnicianName { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
