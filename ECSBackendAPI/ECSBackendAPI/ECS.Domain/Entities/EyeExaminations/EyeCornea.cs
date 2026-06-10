using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.EyeExaminations
{
    /// <summary>
    /// Cornea examination.
    /// </summary>
    public class EyeCornea : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public EyeSide Side { get; set; }

        public string? Clarity { get; set; }
        public string? Size { get; set; }
        public string? Shape { get; set; }
        public decimal? DiameterMm { get; set; }
        public string? Sensation { get; set; }

        // Epithelium
        public bool EpitheliumPunctate { get; set; } = false;
        public string? EpitheliumBullous { get; set; }
        public string? EpitheliumLoss { get; set; }
        public bool BandKeratopathy { get; set; } = false;

        // Stroma
        public string? StromaEdema { get; set; }
        public string? StromaInfiltrate { get; set; }
        public string? StromaThinning { get; set; }
        public bool Ulcer { get; set; } = false;

        // Endothelium
        public string? EndotheliumFolds { get; set; }
        public string? KeraticPrecipitates { get; set; }
        public bool Guttata { get; set; } = false;
        public bool DescemetRupture { get; set; } = false;

        // Perforation
        public bool PerforationThreatened { get; set; } = false;
        public bool Perforation { get; set; } = false;
        public decimal? PerforationDiameterMm { get; set; }

        // Laceration
        public bool Laceration { get; set; } = false;
        public bool? LacerationSutured { get; set; }

        // Neovascularization & Limbal
        public bool Neovascularization { get; set; } = false;
        public bool LimbalStemDeficiency { get; set; } = false;

        // Rare fields (JSONB)
        public string? CorneaExtras { get; set; } // JSONB: drug_deposit, blood_staining, abscess, etc

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
