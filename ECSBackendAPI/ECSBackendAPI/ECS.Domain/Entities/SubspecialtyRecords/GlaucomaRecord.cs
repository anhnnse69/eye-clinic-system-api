using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;

namespace ECS.Domain.Entities.SubspecialtyRecords
{
    public class GlaucomaRecord : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }

        public string? SymptomEyePain { get; set; }
        public string? SymptomBlurredVision { get; set; }
        public bool SymptomVisualFieldConstriction { get; set; } = false;
        public bool SymptomHalos { get; set; } = false;
        public bool SymptomPhotophobia { get; set; } = false;
        public bool SymptomTearing { get; set; } = false;
        public bool SymptomRedEye { get; set; } = false;
        public bool SymptomHeadache { get; set; } = false;
        public bool SymptomNausea { get; set; } = false;
        public bool SymptomVomiting { get; set; } = false;
        public string? SymptomOtherNote { get; set; }

        public bool HistoryMyopia { get; set; } = false;
        public bool HistoryHyperopia { get; set; } = false;
        public bool HistoryTrauma { get; set; } = false;
        public bool HistoryUveitis { get; set; } = false;
        public bool HistoryAnteriorSegmentInflammation { get; set; } = false;
        public bool HistoryCrvo { get; set; } = false;
        public string? HistoryPriorEyeSurgery { get; set; }
        public string? HistoryOtherEyeDisease { get; set; }

        public bool HistorySteroidUse { get; set; } = false;
        public string? SteroidDrugName { get; set; }
        public string? SteroidDuration { get; set; }
        public string? SteroidRoute { get; set; }
        public bool? SteroidSelfMedicated { get; set; }

        public bool SystemicCardiovascular { get; set; } = false;
        public bool SystemicHypertension { get; set; } = false;
        public bool SystemicDiabetes { get; set; } = false;
        public bool SystemicCarotidSinusFistula { get; set; } = false;
        public string? SystemicOtherNote { get; set; }

        public bool FamilyGlaucomaGrandparents { get; set; } = false;
        public bool FamilyGlaucomaParents { get; set; } = false;
        public bool FamilyGlaucomaSiblings { get; set; } = false;
        public bool FamilyGlaucomaOtherRelatives { get; set; } = false;

        public string? GlaucomaType { get; set; }
        public decimal? IopTargetOd { get; set; }
        public decimal? IopTargetOs { get; set; }
        public string? StageOd { get; set; }
        public string? StageOs { get; set; }
        public string? CupDiscDescription { get; set; }
        public string? NerveRimOd { get; set; }
        public string? NerveRimOs { get; set; }
        public string? BlebOdStatus { get; set; }
        public string? BlebOdLocation { get; set; }
        public string? BlebOsStatus { get; set; }
        public string? BlebOsLocation { get; set; }
        public decimal? AcDepthOdSmithMm { get; set; }
        public string? AcDepthOdHerick { get; set; }
        public decimal? AcDepthOsSmithMm { get; set; }
        public string? AcDepthOsHerick { get; set; }
        public string? GonioscopyOd { get; set; }
        public string? GonioscopyOs { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
        public virtual ICollection<GlaucomaSurgeryHistory>? SurgeryHistories { get; set; }
        public virtual ICollection<GlaucomaDrugHistory>? DrugHistories { get; set; }
    }
}
