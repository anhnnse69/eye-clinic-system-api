using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;

namespace ECS.Domain.Entities.EyeExaminations
{
    public class EyeExamination : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }

        public decimal? VaOdUncorrected { get; set; }
        public decimal? VaOsUncorrected { get; set; }
        public decimal? VaOdCorrected { get; set; }
        public decimal? VaOsCorrected { get; set; }
        public decimal? VaOdNear { get; set; }
        public decimal? VaOsNear { get; set; }
        public decimal? VaOdPinhole { get; set; }
        public decimal? VaOsPinhole { get; set; }

        public decimal? IopOdMmhg { get; set; }
        public decimal? IopOsMmhg { get; set; }
        public string? IopMethod { get; set; }

        public string? VisualFieldOd { get; set; }
        public string? VisualFieldOs { get; set; }

        public bool ExtraocularMovementNormal { get; set; } = true;
        public string? ExtraocularMovementNote { get; set; }
        public bool Nystagmus { get; set; } = false;
        public string? NystagmusType { get; set; }
        public string? EyeballOdStatus { get; set; }
        public string? EyeballOsStatus { get; set; }
        public decimal? EyeballOdProptosisMm { get; set; }
        public decimal? EyeballOsProptosisMm { get; set; }

        public bool OrbitOdNormal { get; set; } = true;
        public string? OrbitOdNote { get; set; }
        public bool OrbitOsNormal { get; set; } = true;
        public string? OrbitOsNote { get; set; }

        public string? AutoRefractionOd { get; set; }
        public string? AutoRefractionOs { get; set; }
        public string? RetinoscopyOd { get; set; }
        public string? RetinoscopyOs { get; set; }
        public string? SubjectiveRefractionOd { get; set; }
        public string? SubjectiveRefractionOs { get; set; }

        public bool PreAtropine { get; set; } = false;
        public bool PostAtropine { get; set; } = false;

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
        public virtual ICollection<RefractionRecord>? RefractionRecords { get; set; }
        public virtual ICollection<LacrimalSystem>? LacrimalSystems { get; set; }
        public virtual ICollection<AnteriorSegment>? AnteriorSegments { get; set; }
        public virtual ICollection<PosteriorSegment>? PosteriorSegments { get; set; }
    }
}
