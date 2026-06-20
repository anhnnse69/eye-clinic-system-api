using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.EyeExaminations
{
    /// <summary>
    /// Visual acuity, refraction, intraocular pressure, and basic eye movements.
    /// Covers all record types MS21-MS26
    /// </summary>
    public class EyeExamBasic : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public EyeSide Side { get; set; }

        // ===== VISUAL ACUITY (Thị lực) =====
        public decimal? VaUncorrected { get; set; }
        public decimal? VaCorrected { get; set; }
        public decimal? VaNear { get; set; }
        public decimal? VaPinhole { get; set; }

        // ===== IOP (Nhãn áp) =====
        public decimal? IopMmhg { get; set; }
        public string? IopMethod { get; set; }

        // ===== REFRACTION (Khúc xạ) =====
        public decimal? RefractionSph { get; set; }
        public decimal? RefractionCyl { get; set; }
        public int? RefractionAxis { get; set; }
        public decimal? RefractionAdd { get; set; }
        public decimal? Pd { get; set; }
        public string? AutoRefraction { get; set; }
        public string? Retinoscopy { get; set; }
        public string? SubjectiveRefraction { get; set; }
        public bool PreAtropine { get; set; } = false;
        public bool PostAtropine { get; set; } = false;

        // ===== EXTRAOCULAR MOTILITY (Vận nhãn) =====
        public bool EomNormal { get; set; } = true;
        public string? EomNote { get; set; }
        public bool Nystagmus { get; set; } = false;
        public string? NystagmusType { get; set; }

        // ===== VISUAL FIELD (Thị trường) - MS21 =====
        public string? VisualField { get; set; }

        // ===== EYEBALL & ORBIT (Nhãn cầu & Hốc mắt) =====
        public string? EyeballStatus { get; set; } // Teo, Lồi, Độ lồi - MS25, MS26
        public string? EyeballTexture { get; set; } // Mềm, Căng, To, Nhỏ, Teo - MS26
        public decimal? ProptosisMm { get; set; }
        public bool OrbitNormal { get; set; } = true;
        public string? OrbitNote { get; set; }

        // ===== STRABISMUS (Lác) - MS25 =====
        public string? StrabismusType { get; set; }
        public string? CoverTestResult { get; set; }
        public string? HirschbergTest { get; set; } // Hirschberg test result
        public string? PrismMeasurement { get; set; } // Độ lác đo bằng lăng kính

        // ===== PUPIL EXAMINATION (Đồng tử) - MS25 =====
        public string? PupilExamResult { get; set; }
        public string? PupilReflexLight { get; set; }
        public string? PupilAccommodation { get; set; }
        public string? PupilRelativeAfferentDefect { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
