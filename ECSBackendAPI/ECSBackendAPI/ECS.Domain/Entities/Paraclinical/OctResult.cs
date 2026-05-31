using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;

namespace ECS.Domain.Entities.Paraclinical
{
    public class OctResult : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public string? MachineName { get; set; }
        public string? ScanPattern { get; set; }
        public decimal? RnflAverageOd { get; set; }
        public decimal? RnflAverageOs { get; set; }
        public decimal? CentralMacularThicknessOd { get; set; }
        public decimal? CentralMacularThicknessOs { get; set; }
        public decimal? CupDiscRatioOd { get; set; }
        public decimal? CupDiscRatioOs { get; set; }
        public string? Conclusion { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime ExamDate { get; set; }
        public string? TechnicianName { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
