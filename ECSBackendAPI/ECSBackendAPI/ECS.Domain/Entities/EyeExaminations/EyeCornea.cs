using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.EyeExaminations
{
    /// <summary>
    /// Cornea examination.
    /// Covers MS21, MS22, MS23, MS24, MS25, MS26
    /// </summary>
    public class EyeCornea : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public EyeSide Side { get; set; }

        // Basic properties
        public string? Clarity { get; set; } // Trong, Phù, Sẹo
        public string? Size { get; set; } // Bình thường, To, Nhỏ
        public string? Shape { get; set; } // Bình thường, Nón, Cầu
        public decimal? DiameterMm { get; set; }
        public string? Sensation { get; set; } // Mất, Giảm, Bình thường

        // Epithelium (Biểu mô)
        public bool EpitheliumPunctate { get; set; } = false;
        public string? EpitheliumBullous { get; set; }
        public string? EpitheliumLoss { get; set; }
        public bool BandKeratopathy { get; set; } = false;

        // Stroma (Nhu mô)
        public string? StromaEdema { get; set; }
        public string? StromaInfiltrate { get; set; }
        public string? StromaThinning { get; set; }
        public bool Ulcer { get; set; } = false;
        public string? UlcerLocation { get; set; } // Trung tâm, Lệch tâm, Sát rìa
        public string? UlcerSize { get; set; }

        // Endothelium (Nội mô)
        public string? EndotheliumFolds { get; set; }
        public string? KeraticPrecipitates { get; set; }
        public bool Guttata { get; set; } = false;

        // ===== MS21 TRAUMA SPECIFIC =====
        public bool DescemetRupture { get; set; } = false;
        public string? PosteriorSurfaceDeposit { get; set; } // Tủa mặt sau: Tủa mới, Tủa cũ, Tủa sắc tố
        public string? PosteriorDepositLocation { get; set; }

        // ===== MS22, MS26 SPECIFIC =====
        public string? EpitheliumEdemaLevel { get; set; } // Nhẹ, Vừa, Nặng
        public string? UlcerDescription { get; set; } // Rách gọn, Nham nhở, Mất tổ chức

        // Perforation (Thủng)
        public bool PerforationThreatened { get; set; } = false;
        public bool Perforation { get; set; } = false;
        public decimal? PerforationDiameterMm { get; set; }
        public string? PerforationLocation { get; set; } // Trung tâm, Lệch tâm, Sát rìa
        public string? SeidelTest { get; set; } // Thử Seidel: Thủng bít, Không bít

        // Laceration (Rách)
        public bool Laceration { get; set; } = false;
        public bool? LacerationSutured { get; set; }
        public string? LacerationLocation { get; set; }
        public string? LacerationSize { get; set; }
        public string? LacerationType { get; set; } // Đúng GP, Không đúng GP
        public bool TissueEntrapped { get; set; } = false; // Kẹt tổ chức nội nhãn

        // Neovascularization & Limbal
        public bool Neovascularization { get; set; } = false;
        public string? NeovascularizationLocation { get; set; } // Nông, Sâu
        public string? NeovascularizationExtent { get; set; } // ≤1/3, 1/3-2/3, ≥2/3 chu vi
        public bool LimbalStemDeficiency { get; set; } = false;

        // ===== MS24 GLAUCOMA SPECIFIC =====
        public decimal? CornealThickness { get; set; } // Độ dày giác mạc
        public string? DrugDeposit { get; set; } // Lắng đọng thuốc

        // Rare fields (JSONB)
        public string? CorneaExtras { get; set; } // blood_staining, abscess, etc

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
