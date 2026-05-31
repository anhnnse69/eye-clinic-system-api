using ECS.Domain.Entities.General;

namespace ECS.Domain.Entities.EyeExaminations
{
    public class PosteriorSegment : EntityBase<Guid>
    {
        public Guid ExamId { get; set; }
        public string Side { get; set; } = null!;

        public bool OpticDiscNormal { get; set; } = true;
        public bool OpticDiscEdema { get; set; } = false;
        public bool OpticDiscAtrophy { get; set; } = false;
        public bool OpticDiscPallor { get; set; } = false;
        public string? OpticDiscCupDiscRatio { get; set; }
        public string? OpticDiscRimStatus { get; set; }
        public string? OpticDiscVesselChange { get; set; }
        public bool OpticDiscHemorrhage { get; set; } = false;
        public bool OpticDiscPeripapillaryAtrophy { get; set; } = false;
        public bool OpticDiscNeovascularization { get; set; } = false;
        public string? OpticDiscNvSize { get; set; }
        public bool OpticDiscNotVisible { get; set; } = false;
        public string? OpticDiscOtherNote { get; set; }

        public bool MaculaNormal { get; set; } = true;
        public bool MaculaReflexAbsent { get; set; } = false;
        public string? MaculaEdemaType { get; set; }
        public string? MaculaHoleDegree { get; set; }
        public string? MaculaHoleType { get; set; }
        public bool MaculaScar { get; set; } = false;
        public bool MaculaSerousDetachment { get; set; } = false;
        public bool MaculaRpeDetachment { get; set; } = false;
        public string? MaculaOtherNote { get; set; }

        public bool VesselNormal { get; set; } = true;
        public string? ArteryOcclusionType { get; set; }
        public string? VeinOcclusionType { get; set; }
        public bool OcclusionEdema { get; set; } = false;
        public bool OcclusionIschemia { get; set; } = false;
        public bool OcclusionMixed { get; set; } = false;
        public bool CapillaryPhlebitis { get; set; } = false;
        public bool RetinalNeovascularization { get; set; } = false;
        public bool ChoroidalNeovascularizationSubfoveal { get; set; } = false;
        public bool ChoroidalNeovascularizationExtrafoveal { get; set; } = false;
        public string? VesselOtherNote { get; set; }

        public bool RetinaHemorrhageSuperficial { get; set; } = false;
        public bool RetinaHemorrhageDeep { get; set; } = false;
        public bool RetinaHemorrhageChoroidal { get; set; } = false;
        public bool RetinaExudateHard { get; set; } = false;
        public bool RetinaExudateCottonWool { get; set; } = false;
        public bool RetinaEdema { get; set; } = false;
        public bool RetinaDegenerationPeripheral { get; set; } = false;
        public bool RetinaDegenerationCentral { get; set; } = false;
        public string? RetinaDegenerationType { get; set; }
        public bool ChorioretinitisActive { get; set; } = false;
        public bool ChorioretinitisScar { get; set; } = false;
        public int? ChorioretinitisCount { get; set; }
        public string? ChorioretinitisLocation { get; set; }
        public bool RetinalDetachment { get; set; } = false;
        public string? RetinalDetachmentDegree { get; set; }
        public bool RetinalTear { get; set; } = false;
        public int? RetinalTearCount { get; set; }
        public string? RetinalTearLocation { get; set; }
        public string? RetinalTearShape { get; set; }
        public bool IntraocularForeignBody { get; set; } = false;
        public string? ForeignBodySize { get; set; }
        public string? ForeignBodyLocation { get; set; }
        public string? RetinaOtherNote { get; set; }

        public virtual EyeExamination Examination { get; set; } = null!;
    }
}
