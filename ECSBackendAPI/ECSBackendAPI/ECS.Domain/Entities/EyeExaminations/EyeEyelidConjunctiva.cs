using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.EyeExaminations
{
    /// <summary>
    /// Eyelid, Conjunctiva & Lacrimal examination.
    /// Covers MS21 (Trauma), MS22 (Anterior), MS26 (Pediatric)
    /// </summary>
    public class EyeEyelidConjunctiva : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public EyeSide Side { get; set; }

        // ===== EYELID (Mi mắt) =====
        public bool EyelidNormal { get; set; } = true;
        public bool EyelidEdema { get; set; } = false;
        public bool EyelidHemorrhage { get; set; } = false;

        // Sụp mi
        public bool Ptosis { get; set; } = false;
        public string? PtosisDegree { get; set; }

        // Rách mi (Laceration)
        public bool Laceration { get; set; } = false;
        public string? LacerationExtent { get; set; } // Lớp, Toàn bộ chiều dày, Bờ mi, Mất tổ chức
        public string? LacerationDepth { get; set; }
        public bool LacerationUnsutured { get; set; } = false;
        public bool LacerationSutured { get; set; } = false;

        // Lệ quản (Canaliculus) - MS21
        public bool LacrimalDuctNormal { get; set; } = true;
        public bool LacrimalDuctCut { get; set; } = false;
        public string? LacrimalDuctCutLocation { get; set; } // 1/3 ngoài, 1/3 giữa, 1/3 trong, Đứt 2 lệ quản

        // Other eyelid conditions
        public bool Entropion { get; set; } = false;
        public bool Ectropion { get; set; } = false;
        public bool Lagophthalmos { get; set; } = false;
        public bool Scar { get; set; } = false;
        public bool Chalazion { get; set; } = false;
        public bool Hordeolum { get; set; } = false;
        public string? EyelidOther { get; set; } // JSONB

        // ===== CONJUNCTIVA (Kết mạc) =====
        public bool ConjunctivaNormal { get; set; } = true;
        public string? ConjunctivaCongestionType { get; set; } // Toả lan, Ở nhãn cầu, Rìa
        public bool ConjunctivaEdema { get; set; } = false;
        public bool ConjunctivaHemorrhage { get; set; } = false;
        public string? ConjunctivaHemorrhageLocation { get; set; }

        public bool ConjunctivaPapilla { get; set; } = false;
        public bool ConjunctivaFollicle { get; set; } = false;
        public bool ConjunctivaKeratinization { get; set; } = false;
        public bool ConjunctivaScar { get; set; } = false;
        public bool ConjunctivaLaceration { get; set; } = false;
        public string? ConjunctivaLacerationLocation { get; set; }
        public string? ConjunctivaDischarge { get; set; } // Tiết tố mủ, Tiết tố trong, Giả mạc
        public bool FluoresceinStain { get; set; } = false;
        public string? ConjunctivaOther { get; set; } // JSONB

        // ===== FORNIX & SYMBLEPHARON (Cùng đồ) - MS22, MS26 =====
        public string? FornixStatus { get; set; } // Bình thường, Cạn, Dính
        public string? SymblepharonHeight { get; set; } // Chiều cao cầu dính
        public string? SymblepharonWidth { get; set; } // Độ rộng cầu dính

        // ===== PTERYGIUM (Mắt ngả) - MS22 =====
        public bool Pterygium { get; set; } = false;
        public string? PterygiumLocation { get; set; }
        public string? PterygiumSize { get; set; }

        // ===== TUMOR (U) - MS22 =====
        public bool HasTumor { get; set; } = false;
        public string? TumorNature { get; set; }
        public string? TumorLocation { get; set; }
        public string? TumorSize { get; set; }

        // ===== ENTROPION/EPICANTHUS (Quặm/Thẩm lệ) - MS26 =====
        public bool EntropionPediatric { get; set; } = false;
        public bool Epicanthus { get; set; } = false;
        public string? EpicanthusType { get; set; } // 1/3 trong, 1/3 giữa, 1/3 ngoài, Toàn bộ

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
