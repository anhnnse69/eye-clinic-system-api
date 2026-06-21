using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;

namespace ECS.Domain.Entities.SubspecialtyRecords
{
    /// <summary>
    /// Strabismus & Ptosis record - MS25 (Lác, sụp mi).
    /// Contains comprehensive strabismus and ptosis examination data.
    /// </summary>
    public class StrabismusPtosisRecord : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }

        // ===== CHIEF COMPLAINT & CAUSE =====
        public bool ChiefStrabismus { get; set; } = false;
        public bool ChiefPtosis { get; set; } = false;
        public bool Congenital { get; set; } = false;
        public bool Acquired { get; set; } = false;
        public string? AcquiredOnset { get; set; }

        // ===== STRABISMUS TYPE =====
        public string? StrabismusType { get; set; } // Lác trong, Lác ngoài, Lác chéo

        // ===== NYSTAGMUS =====
        public bool Nystagmus { get; set; } = false;
        public string? NystagmusType { get; set; }

        // ===== TREATMENT HISTORY =====
        public string? PriorAmblyopiaTreatment { get; set; }
        public string? PriorAmblyopiaResult { get; set; } // Tốt, Trung bình, Kém
        public string? PriorSurgery { get; set; }
        public string? PriorSurgeryResult { get; set; } // Tốt, Mổ non, Mổ già

        // ===== VISUAL ACUITY BEFORE/AFTER ATROPINE =====
        public string? VaBeforeAtropineOd { get; set; }
        public string? VaBeforeAtropineOs { get; set; }
        public string? VaAfterAtropineOd { get; set; }
        public string? VaAfterAtropineOs { get; set; }

        // ===== REFRACTION =====
        public string? RefractionPreAtropine { get; set; }
        public string? RefractionPostAtropine { get; set; }

        // ===== PUPIL SHADOW TEST (Soi bóng đồng tử) =====
        public string? PupilShadowTestOd { get; set; }
        public string? PupilShadowTestOs { get; set; }

        // ===== EXTRAOCULAR MOTILITY (Vận nhãn ngoại lai) =====
        public string? EomGazeTest { get; set; } // Gia tăng (+), (++), (+++), Hạn chế (-), (--), (---)

        // ===== INTERNAL EXTRAOCULAR MOTILITY (Vận nhãn nội tại) =====
        public string? EomInternalOd { get; set; }
        public string? EomInternalOs { get; set; }

        // ===== CONVERGENCE POINT (Điểm cận quỹ) =====
        public string? ConvergencePoint { get; set; }

        // ===== COVER TEST =====
        public string? CoverTestResult { get; set; } // Trả trong ra, Trả ngoài vào, Trả chéo

        // ===== HIRSCHBERG TEST =====
        public string? HirschbergBeforeAtropine { get; set; }
        public string? HirschbergAfterAtropine { get; set; }

        // ===== PRISM MEASUREMENT =====
        public string? PrismNear { get; set; }
        public string? PrismDistance { get; set; }
        public string? PrismUp { get; set; }
        public string? PrismDown { get; set; }

        // ===== SYNDROME =====
        public string? StrabismusSyndrome { get; set; }

        // ===== SYOPTOPHORE TEST =====
        public string? SynoptophoreObjective { get; set; }
        public string? SynoptophoreSubjective { get; set; }

        // ===== BINOCULAR VISION (Thị giác hai mắt) =====
        public string? BinocularStatus { get; set; } // Đồng thị, Hợp thị, Phù thị
        public string? FusionAmplitude { get; set; } // Biên độ hợp thị
        public string? RetinalCorrespondence { get; set; } // Tương ứng võng mạc
        public string? Diplopia { get; set; } // Song thị
        public string? CompensatoryHeadPosture { get; set; } // Tư thế bù trừ

        // ===== PTOSIS MEASUREMENTS =====
        public string? PtosisDegreeOd { get; set; }
        public string? PtosisDegreeOs { get; set; }
        public string? LevatorFunctionOd { get; set; }
        public string? LevatorFunctionOs { get; set; }
        public string? MarcusGunn { get; set; }
        public string? BellPhenomenon { get; set; }
        public string? FixationOd { get; set; }
        public string? FixationOs { get; set; }

        // ===== PALPEBRAL REFLEX (Phản xạ thể mi) =====
        public string? PalpebralReflexOd { get; set; }
        public string? PalpebralReflexOs { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
