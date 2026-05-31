using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;

namespace ECS.Domain.Entities.SubspecialtyRecords
{
    public class StrabismusPtosisRecord : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }

        public bool ChiefStrabismus { get; set; } = false;
        public bool ChiefPtosis { get; set; } = false;
        public string? ChiefOther { get; set; }

        public bool Congenital { get; set; } = false;
        public bool Acquired { get; set; } = false;
        public string? AcquiredOnset { get; set; }
        public string? PriorAmblyopiaTreatment { get; set; }
        public string? PriorSurgeryMethod { get; set; }
        public string? PriorSurgeryResult { get; set; }

        public bool Esotropia { get; set; } = false;
        public bool Exotropia { get; set; } = false;
        public bool VerticalStrabismus { get; set; } = false;
        public bool Nystagmus { get; set; } = false;
        public string? NystagmusType { get; set; }
        public bool NullPointNystagmus { get; set; } = false;

        public string? AutoRefractionPreAtropineOd { get; set; }
        public string? AutoRefractionPreAtropineOs { get; set; }
        public string? AutoRefractionPostAtropineOd { get; set; }
        public string? AutoRefractionPostAtropineOs { get; set; }
        public string? RetinoscopyPostAtropineOd { get; set; }
        public string? RetinoscopyPostAtropineOs { get; set; }

        public string? ExtraocularMotilityOd { get; set; }
        public string? ExtraocularMotilityOs { get; set; }

        public bool IntrinsicMotilityOdNormal { get; set; } = true;
        public string? IntrinsicMotilityOdNote { get; set; }
        public bool IntrinsicMotilityOsNormal { get; set; } = true;
        public string? IntrinsicMotilityOsNote { get; set; }

        public bool NearPointConvergenceNormal { get; set; } = true;
        public decimal? NearPointConvergenceCm { get; set; }

        public string? CoverTestResult { get; set; }

        public decimal? HirschbergOdPre { get; set; }
        public decimal? HirschbergOdPost { get; set; }
        public decimal? HirschbergOsPre { get; set; }
        public decimal? HirschbergOsPost { get; set; }
        public decimal? PrismNearOd { get; set; }
        public decimal? PrismDistanceOd { get; set; }
        public decimal? PrismUpOd { get; set; }
        public decimal? PrismDownOd { get; set; }
        public decimal? PrismNearOs { get; set; }
        public decimal? PrismDistanceOs { get; set; }
        public decimal? PrismUpOs { get; set; }
        public decimal? PrismDownOs { get; set; }
        public string? StrabismusSyndrome { get; set; }
        public string? StrabismusNature { get; set; }

        public decimal? SynoptophoreObjective { get; set; }
        public decimal? SynoptophoreSubjective { get; set; }

        public bool BinocularSimultaneousVision { get; set; } = false;
        public bool BinocularFusion { get; set; } = false;
        public bool BinocularStereopsis { get; set; } = false;
        public string? FusionAmplitude { get; set; }
        public bool RetinalCorrespondenceNormal { get; set; } = true;
        public bool Diplopia { get; set; } = false;
        public string? DiplopiaNote { get; set; }
        public bool CompensatoryHeadPosture { get; set; } = false;
        public string? CompensatoryHeadPostureNote { get; set; }

        public bool Ptosis { get; set; } = false;
        public string? PtosisDegreeOd { get; set; }
        public string? PtosisDegreeOs { get; set; }
        public bool EpicanthusOd { get; set; } = false;
        public bool EpicanthusOs { get; set; } = false;
        public string? LevatorFunctionOd { get; set; }
        public string? LevatorFunctionOs { get; set; }
        public bool MarcusGunnOd { get; set; } = false;
        public bool MarcusGunnOs { get; set; } = false;
        public bool BellPhenomenonOd { get; set; } = false;
        public bool BellPhenomenonOs { get; set; } = false;

        public string? FixationOd { get; set; }
        public string? FixationOs { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
