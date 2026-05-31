using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;

namespace ECS.Domain.Entities.Paraclinical
{
    public class VisualFieldTest : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public string? Machine { get; set; }
        public string? Strategy { get; set; }
        public string Side { get; set; } = null!;
        public decimal? MdValue { get; set; }
        public decimal? PsdValue { get; set; }
        public decimal? VfiPercent { get; set; }
        public bool Reliable { get; set; } = false;
        public string? ResultSummary { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime TestDate { get; set; }
        public string? TechnicianName { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }

}
