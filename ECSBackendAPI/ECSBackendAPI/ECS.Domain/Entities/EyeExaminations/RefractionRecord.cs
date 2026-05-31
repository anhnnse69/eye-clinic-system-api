using ECS.Domain.Entities.General;

namespace ECS.Domain.Entities.EyeExaminations
{
    public class RefractionRecord : EntityBase<Guid>
    {
        public Guid ExamId { get; set; }
        public decimal? SphOd { get; set; }
        public decimal? CylOd { get; set; }
        public int? AxisOd { get; set; }
        public decimal? AddOd { get; set; }
        public decimal? SphOs { get; set; }
        public decimal? CylOs { get; set; }
        public int? AxisOs { get; set; }
        public decimal? AddOs { get; set; }
        public decimal? PdBinocular { get; set; }
        public decimal? PdOd { get; set; }
        public decimal? PdOs { get; set; }
        public string? Method { get; set; }
        public bool PreAtropine { get; set; } = false;
        public bool PostAtropine { get; set; } = false;
        public string? LensType { get; set; }
        public string? Notes { get; set; }

        public virtual EyeExamination Examination { get; set; } = null!;
    }
}
