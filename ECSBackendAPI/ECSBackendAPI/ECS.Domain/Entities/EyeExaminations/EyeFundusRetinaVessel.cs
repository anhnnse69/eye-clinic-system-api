using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.EyeExaminations
{
    /// <summary>
    /// Fundus examination - Retina & Blood vessels.
    /// </summary>
    public class EyeFundusRetinaVessel : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public EyeSide Side { get; set; }

        // Blood vessels
        public bool VesselNormal { get; set; } = true;
        public string? ArteryOcclusionType { get; set; }
        public string? VeinOcclusionType { get; set; }
        public bool OcclusionEdema { get; set; } = false;
        public bool OcclusionIschemia { get; set; } = false;
        public bool ChoroidalNeovascularization { get; set; } = false;

        // Retina
        public bool RetinaHemorrhageSuperficial { get; set; } = false;
        public bool RetinaHemorrhageDeep { get; set; } = false;
        public bool RetinaExudateHard { get; set; } = false;
        public bool RetinaExudateCottonWool { get; set; } = false;
        public bool RetinaEdema { get; set; } = false;
        public bool RetinaDegenerationPeripheral { get; set; } = false;
        public bool RetinaDegenerationCentral { get; set; } = false;

        // Retinal detachment/tear
        public bool RetinalDetachment { get; set; } = false;
        public bool RetinalTear { get; set; } = false;
        public int? RetinalTearCount { get; set; }

        // Chorioretinitis (MS23)
        public bool ChorioretinitisActive { get; set; } = false;
        public bool ChorioretinitisScar { get; set; } = false;
        public int? ChorioretinitisCount { get; set; }

        // Intraocular foreign body (MS21)
        public bool IntraocularForeignBody { get; set; } = false;

        public string? RetinaVesselExtras { get; set; } // JSONB

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
