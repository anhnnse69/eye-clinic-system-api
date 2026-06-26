using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.EyeExaminations
{
    /// <summary>
    /// Anterior Chamber, Iris & Pupil examination.
    /// Covers MS21, MS22, MS23, MS24, MS25, MS26
    /// </summary>
    public class EyeAcIris : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public EyeSide Side { get; set; }

        // ===== ANTERIOR CHAMBER (Tiền phòng) =====
        public decimal? AcDepthMm { get; set; }
        public string? AcDepthHerick { get; set; } // <1/4, 1/4-1/2, ≥1/2 GM
        public bool AcFlat { get; set; } = false;
        public bool AcLensMaterial { get; set; } = false;
        public bool AcPus { get; set; } = false;
        public decimal? AcPusMm { get; set; }
        public bool AcExudate { get; set; } = false;
        public string? AcExudateDescription { get; set; }
        public bool AcHemorrhage { get; set; } = false;
        public string? AcHemorrhageLevel { get; set; }
        public bool AcForeignBody { get; set; } = false;
        public string? AcTyndall { get; set; }
        public string? AcOtherFindings { get; set; } // Chất thể thủy tinh, Mủ, Xuất tiết

        // ===== ANGLE (Góc tiền phòng) - MS22, MS24 =====
        public bool AngleSynechiae { get; set; } = false;
        public bool AnglePigment { get; set; } = false;
        public bool AngleNeovascularization { get; set; } = false;
        public string? AngleOtherFindings { get; set; }

        // ===== IRIS (Mống mắt) =====
        public string? IrisColor { get; set; }
        public string? IrisCondition { get; set; } // Bình thường, Thoái hóa, Nâu xốp, Xơ teo
        public bool IrisDegeneration { get; set; } = false;
        public bool IrisNeovascularization { get; set; } = false;
        public bool IrisCiliaryProcesses { get; set; } = false; // Phòi
        public bool IrisKoeppeNodules { get; set; } = false;
        public bool IrisBusaccaNodules { get; set; } = false;
        public bool IrisProlapse { get; set; } = false;
        public bool IrisRootTear { get; set; } = false;
        public string? IrisRootTearDegree { get; set; }
        public bool IrisLoss { get; set; } = false;
        public bool IrisPerforation { get; set; } = false;
        public string? IrisTumorLocation { get; set; } // MS26

        // ===== PUPIL (Đồng tử) =====
        public bool PupilRound { get; set; } = true;
        public bool PupilIrregular { get; set; } = false;
        public decimal? PupilDiameterMm { get; set; }
        public bool PupilSychiae { get; set; } = false;
        public string? PupilSynechiaeLocation { get; set; }
        public string? PupilReflex { get; set; } // Tốt, Kém, Mất
        public string? PupilLightReflex { get; set; }
        public string? PupilPtdtTest { get; set; } // Phản xạ đồng tử đảo: Có, Không
        public bool PupilDilated { get; set; } = false;
        public bool PupilParalyzed { get; set; } = false;

        // ===== FUNDUS REFLEX (Ánh đồng tử) =====
        public string? FundusReflex { get; set; } // Hồng, Xám, Không soi được

        public string? AcIrisExtras { get; set; } // JSONB

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
