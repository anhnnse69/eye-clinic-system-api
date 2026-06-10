using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.EyeExaminations
{
    /// <summary>
    /// Anterior chamber & Iris examination.
    /// </summary>
    public class EyeAcIris : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public EyeSide Side { get; set; }

        // Anterior Chamber
        public decimal? AcDepthMm { get; set; }
        public string? AcDepthHerick { get; set; }
        public bool AcFlat { get; set; } = false;
        public bool AcLensMaterial { get; set; } = false;
        public decimal? AcPusMm { get; set; }
        public string? AcTyndall { get; set; }
        public bool AcHemorrhage { get; set; } = false;

        // Angle
        public bool AngleSynechiae { get; set; } = false;
        public bool AnglePigment { get; set; } = false;
        public bool AngleNeovascularization { get; set; } = false;

        // Iris
        public string? IrisColor { get; set; }
        public bool IrisDegeneration { get; set; } = false;
        public bool IrisNeovascularization { get; set; } = false;
        public bool IrisKoeppeNodules { get; set; } = false;
        public bool IrisBusaccaNodules { get; set; } = false;
        public bool IrisProlapse { get; set; } = false;
        public bool IrisRootTear { get; set; } = false;
        public string? IrisRootTearDegree { get; set; }
        public bool IrisLoss { get; set; } = false;
        public bool IrisPerforation { get; set; } = false;

        // Pupil
        public bool PupilRound { get; set; } = true;
        public bool PupilIrregular { get; set; } = false;
        public decimal? PupilDiameterMm { get; set; }
        public bool PupilSychiae { get; set; } = false;
        public string? PupilReflex { get; set; }
        public string? PupilLightReflex { get; set; }
        public string? FundusReflex { get; set; }

        public string? AcIrisExtras { get; set; } // JSONB

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
