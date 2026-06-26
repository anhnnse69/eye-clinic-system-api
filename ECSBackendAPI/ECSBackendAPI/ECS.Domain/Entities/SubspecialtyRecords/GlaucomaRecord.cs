using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.SubspecialtyRecords
{
    /// <summary>
    /// Glaucoma record - MS24 (Glôcôm).
    /// Contains comprehensive glaucoma examination and management data.
    /// </summary>
    public class GlaucomaRecord : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }

        // ===== SYMPTOMS (Triệu chứng) =====
        public string? EyePainLevel { get; set; } // dữ dội, vừa, nhẹ, không
        public string? VisionSymptoms { get; set; } // mờ đột ngột, mờ từng lúc, sương mù, không mờ
        public string? VisionProgression { get; set; } // mờ tăng dần, nhìn thu hẹp, quầng tán sắc
        public bool HasPhotophobia { get; set; } = false;
        public bool HasTearing { get; set; } = false;
        public bool HasRedness { get; set; } = false;
        public string? SystemicSymptoms { get; set; } // đau đầu, nôn, buồn nôn

        // ===== VISUAL ACUITY & IOP =====
        public decimal? VaWithoutCorrectionOd { get; set; }
        public decimal? VaWithoutCorrectionOs { get; set; }
        public decimal? VaWithCorrectionOd { get; set; }
        public decimal? VaWithCorrectionOs { get; set; }
        public decimal? IopOd { get; set; }
        public decimal? IopOs { get; set; }
        public string? IopMethod { get; set; } // Maclakov, Goldmann
        public decimal? IopTargetOd { get; set; }
        public decimal? IopTargetOs { get; set; }

        // ===== HISTORY (Tiền sử) =====
        public string? HistoryEye { get; set; } // Cận thị, Viễn thị, Chấn thương, Viêm màng bồ đào, Tắc TMTTVM
        public string? HistoryEyeSurgery { get; set; }
        public string? PriorEyeSurgeryDetails { get; set; }
        public string? SteroidUse { get; set; } // Tên thuốc, thời gian, đường dùng
        public string? SteroidPrescribed { get; set; } // Theo chỉ định BS hoặc tự dùng

        // MS24 Specific History Fields (top-level DTO)
        /// <summary>
        /// Duration of glaucoma symptoms.
        /// </summary>
        public string? GlaucomaSymptomDuration { get; set; }
        /// <summary>
        /// Previously visited healthcare facilities.
        /// </summary>
        public string? GlaucomaPriorFacility { get; set; }
        /// <summary>
        /// Prior treatment methods.
        /// </summary>
        public string? GlaucomaPriorTreatment { get; set; }
        /// <summary>
        /// Other eye disease history.
        /// </summary>
        public string? GlaucomaHistoryEye { get; set; }
        /// <summary>
        /// Family history of glaucoma.
        /// </summary>
        public string? GlaucomaFamilyHistory { get; set; }

        // Systemic history
        public bool HasCardiovascularDisease { get; set; } = false;
        public bool HasHypertension { get; set; } = false;
        public bool HasDiabetes { get; set; } = false;
        public bool HasCarotidFistula { get; set; } = false;
        public string? OtherSystemicDisease { get; set; }

        // Family history
        public bool FamilyHasGlaucoma { get; set; } = false;
        public string? FamilyGlaucomaRelation { get; set; } // ông bà, bố mẹ, anh chị em, cô dì chú bác

        // ===== TREATMENT HISTORY =====
        public string? GlaucomaMedications { get; set; } // Tên thuốc, liều dùng, thời gian đã dùng
        public string? MedicationChangeReason { get; set; }
        public string? OtherMedications { get; set; }
        public string? TreatmentProgress { get; set; }

        // ===== CLASSIFICATION =====
        public string? GlaucomaType { get; set; }
        public string? StageOd { get; set; }
        public string? StageOs { get; set; }

        // ===== EXAMINATION =====
        // Eyelid
        public bool HasEyelidSwelling { get; set; } = false;

        // Conjunctiva
        public bool HasConjunctivalInjection { get; set; } = false;
        public bool HasFilteringBleb { get; set; } = false;
        public string? BlebLocation { get; set; }
        public string? BlebStatus { get; set; } // tốt, dẹt, xơ, mỏng, quá phát

        // Cornea
        public string? CornealTransparency { get; set; } // trong, sẹo, phù
        public decimal? CornealThickness { get; set; }

        // Sclera
        public bool HasScleralThinning { get; set; } = false;
        public string? ScleralScarLocation { get; set; }

        // Anterior Chamber
        public string? AcDepthSmith { get; set; }
        public string? AcDepthHerick { get; set; }

        // Gonioscopy
        public string? GonioscopyOd { get; set; }
        public string? GonioscopyOs { get; set; }
        public string? AngleFindings { get; set; }

        // Iris
        public string? IrisColor { get; set; }
        public string? IrisCondition { get; set; } // Thoái hoá
        public bool HasIrisNeovascularization { get; set; } = false;

        // Pupil
        public string? PupilDiameter { get; set; }
        public string? PupilPigmentBorder { get; set; }
        public string? PupilReflexResponse { get; set; } // bình thường, giảm, mất

        // Lens
        public string? LensStatus { get; set; } // trong, đục

        // Fundus findings
        public string? FundusRetinaFindings { get; set; }
        public string? FundusMaculaFindings { get; set; }
        public bool HasCNV { get; set; } = false;
        public bool HasRetinalHemorrhage { get; set; } = false;

        // Optic disc
        public string? OpticDiscDescription { get; set; }
        public string? NerveRimOd { get; set; }
        public string? NerveRimOs { get; set; }
        public string? OpticDiscCupRatio { get; set; }
        public string? OpticDiscVesselChange { get; set; }
        public bool HasOpticDiscHemorrhage { get; set; } = false;
        public bool HasRimAtrophy { get; set; } = false;

        // ===== EYE MEASUREMENTS =====
        public string? EyeAxialLength { get; set; }

        // ===== TREATMENT PLAN =====
        public string? TreatmentPlanSurgery { get; set; }
        public string? TreatmentPlanLaser { get; set; }
        public string? TreatmentPlanMedication { get; set; }
        public string? FollowUpPlan { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
        public virtual ICollection<GlaucomaHistory>? Histories { get; set; }
    }
}
