using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;

namespace ECS.Domain.Entities.SubspecialtyRecords
{
    public class PediatricEyeRecord : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }

        public bool Congenital { get; set; } = false;
        public bool Acquired { get; set; } = false;
        public string? AcquiredOnset { get; set; }
        public string? PriorTreatment { get; set; }

        public bool PregnancyIllness { get; set; } = false;
        public string? PregnancyIllnessDetail { get; set; }
        public bool IntellectualDevelopmentNormal { get; set; } = true;
        public string? IntellectualDevelopmentNote { get; set; }

        public bool ChiefBlurredVision { get; set; } = false;
        public bool ChiefEyePain { get; set; } = false;
        public bool ChiefRedEye { get; set; } = false;
        public bool ChiefPhotophobia { get; set; } = false;

        public bool ExtraocularMotilityOdNormal { get; set; } = true;
        public string? ExtraocularMotilityOdNote { get; set; }
        public bool ExtraocularMotilityOsNormal { get; set; } = true;
        public string? ExtraocularMotilityOsNote { get; set; }
        public bool IntrinsicMotilityOdNormal { get; set; } = true;
        public string? IntrinsicMotilityOdNote { get; set; }
        public bool IntrinsicMotilityOsNormal { get; set; } = true;
        public string? IntrinsicMotilityOsNote { get; set; }
        public bool Nystagmus { get; set; } = false;
        public string? NystagmusNote { get; set; }

        public bool EntropionOd { get; set; } = false;
        public string? EntropionOdLocation { get; set; }
        public bool EntropionOs { get; set; } = false;
        public string? EntropionOsLocation { get; set; }
        public bool EpicanthusOd { get; set; } = false;
        public bool EpicanthusOs { get; set; } = false;
        public bool PtosisOd { get; set; } = false;
        public bool PtosisOs { get; set; } = false;
        public bool EyelidTumorOd { get; set; } = false;
        public bool EyelidTumorOs { get; set; } = false;
        public string? EyelidOtherOd { get; set; }
        public string? EyelidOtherOs { get; set; }

        public string? LacrimalOdNote { get; set; }
        public string? LacrimalOsNote { get; set; }

        public bool ConjunctivaCongestionOd { get; set; } = false;
        public bool ConjunctivaHemorrhageOd { get; set; } = false;
        public bool ConjunctivaExudateOd { get; set; } = false;
        public bool ConjunctivaTumorOd { get; set; } = false;
        public string? ConjunctivaOtherOd { get; set; }
        public bool ConjunctivaCongestionOs { get; set; } = false;
        public bool ConjunctivaHemorrhageOs { get; set; } = false;
        public bool ConjunctivaExudateOs { get; set; } = false;
        public bool ConjunctivaTumorOs { get; set; } = false;
        public string? ConjunctivaOtherOs { get; set; }

        public string? CorneaClarityOd { get; set; }
        public string? CorneaEdemaOd { get; set; }
        public string? CorneaTumorOd { get; set; }
        public string? CorneaPrecipitatesOd { get; set; }
        public string? CorneaUlcerOd { get; set; }
        public string? CorneaMalformationOd { get; set; }
        public decimal? CorneaDiameterOdMm { get; set; }
        public string? CorneaLimbusOd { get; set; }
        public string? CorneaOtherOd { get; set; }
        public string? CorneaClarityOs { get; set; }
        public string? CorneaEdemaOs { get; set; }
        public string? CorneaTumorOs { get; set; }
        public string? CorneaPrecipitatesOs { get; set; }
        public string? CorneaUlcerOs { get; set; }
        public string? CorneaMalformationOs { get; set; }
        public decimal? CorneaDiameterOsMm { get; set; }
        public string? CorneaLimbusOs { get; set; }
        public string? CorneaOtherOs { get; set; }

        public bool ScleraEctasiaOd { get; set; } = false;
        public bool ScleraScarOd { get; set; } = false;
        public string? ScleraCongestionOd { get; set; }
        public bool ScleraInflammationOd { get; set; } = false;
        public string? ScleraOtherOd { get; set; }
        public bool ScleraEctasiaOs { get; set; } = false;
        public bool ScleraScarOs { get; set; } = false;
        public string? ScleraCongestionOs { get; set; }
        public bool ScleraInflammationOs { get; set; } = false;
        public string? ScleraOtherOs { get; set; }

        public decimal? AcDepthOdMm { get; set; }
        public string? AcTyndallOd { get; set; }
        public bool AcPusOd { get; set; } = false;
        public bool AcBloodOd { get; set; } = false;
        public string? AcAngleOd { get; set; }
        public decimal? AcDepthOsMm { get; set; }
        public string? AcTyndallOs { get; set; }
        public bool AcPusOs { get; set; } = false;
        public bool AcBloodOs { get; set; } = false;
        public string? AcAngleOs { get; set; }

        public string? IrisColorOd { get; set; }
        public bool IrisDegenerationOd { get; set; } = false;
        public bool IrisNeovascularizationOd { get; set; } = false;
        public bool CiliarySensationOd { get; set; } = true;
        public bool IrisTumorOd { get; set; } = false;
        public bool IrisMalformationOd { get; set; } = false;
        public string? IrisOtherOd { get; set; }
        public string? IrisColorOs { get; set; }
        public bool IrisDegenerationOs { get; set; } = false;
        public bool IrisNeovascularizationOs { get; set; } = false;
        public bool CiliarySensationOs { get; set; } = true;
        public bool IrisTumorOs { get; set; } = false;
        public bool IrisMalformationOs { get; set; } = false;
        public string? IrisOtherOs { get; set; }

        public string? PupilShapeOd { get; set; }
        public decimal? PupilDiameterOdMm { get; set; }
        public string? PupilPigmentRuffOd { get; set; }
        public string? PupilReflexOd { get; set; }
        public string? PupilMalformationOd { get; set; }
        public string? PupilShapeOs { get; set; }
        public decimal? PupilDiameterOsMm { get; set; }
        public string? PupilPigmentRuffOs { get; set; }
        public string? PupilReflexOs { get; set; }
        public string? PupilMalformationOs { get; set; }

        public string? LensStatusOd { get; set; }
        public string? LensStatusOs { get; set; }

        public string? VitreousStatusOd { get; set; }
        public string? VitreousStatusOs { get; set; }

        public string? RetinaOd { get; set; }
        public string? MaculaOd { get; set; }
        public string? VesselsOd { get; set; }
        public string? OpticDiscOd { get; set; }
        public string? FundusTumorOd { get; set; }
        public string? RetinaOs { get; set; }
        public string? MaculaOs { get; set; }
        public string? VesselsOs { get; set; }
        public string? OpticDiscOs { get; set; }
        public string? FundusTumorOs { get; set; }

        public string? EyeballOdStatus { get; set; }
        public string? EyeballOsStatus { get; set; }

        public string? AmblyopiaStatus { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
