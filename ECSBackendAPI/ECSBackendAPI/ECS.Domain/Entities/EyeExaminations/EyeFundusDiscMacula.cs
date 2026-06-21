using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.EyeExaminations
{
    /// <summary>
    /// Fundus examination - Optic disc, Macula & Choroid.
    /// Covers MS23 (Fundus), MS24 (Glaucoma)
    /// </summary>
    public class EyeFundusDiscMacula : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public EyeSide Side { get; set; }

        // ===== OPTIC DISC (Đĩa thị) =====
        public bool OpticDiscNormal { get; set; } = true;
        public string? OpticDiscColor { get; set; } // Bình thường, Đỏ, Bạc màu
        public bool OpticDiscEdema { get; set; } = false;
        public bool OpticDiscAtrophy { get; set; } = false;
        public bool OpticDiscPallor { get; set; } = false;
        public string? OpticDiscCupRatio { get; set; } // C/D
        public string? OpticDiscRimStatus { get; set; } // Viền thần kinh: bình thường, bất thường
        public string? OpticDiscRimLocation { get; set; } // dưới, trên, mũi, thái dương
        public string? OpticDiscVesselChange { get; set; } // bình thường, chuyển hướng, gập góc
        public bool OpticDiscHemorrhage { get; set; } = false;
        public bool OpticDiscNeovascularization { get; set; } = false;
        public bool OpticDiscNotVisible { get; set; } = false;

        // ===== MACULA (Hoàng điểm) =====
        public bool MaculaNormal { get; set; } = true;
        public string? MaculaCondition { get; set; } // Bình thường, Phù, Lõm teo
        public bool MaculaReflexAbsent { get; set; } = false;
        public string? MaculaEdemaType { get; set; }
        public string? MaculaHoleDegree { get; set; }
        public bool MaculaScar { get; set; } = false;
        public bool MaculaSerousDetachment { get; set; } = false;

        // ===== CHOROID (Hắc mạc) - MS24 =====
        public bool ChoroidalNormal { get; set; } = true;
        public string? ChoroidalFindings { get; set; }

        // ===== SUPPLEMENTARY FINDINGS =====
        public string? DiscMaculaExtras { get; set; } // JSONB

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
