using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.EyeExaminations
{
    /// <summary>
    /// Fundus examination - Retina & Blood vessels.
    /// Covers MS21, MS23, MS24, MS25, MS26
    /// </summary>
    public class EyeFundusRetinaVessel : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public EyeSide Side { get; set; }

        // ===== BLOOD VESSELS (Hệ mạch máu) =====
        public bool VesselNormal { get; set; } = true;
        public string? VesselStatus { get; set; } // Tình trạng mạch: Bình thường, Tắc động mạch, Tắc tĩnh mạch
        public string? ArteryOcclusionType { get; set; } // trung tâm, nhánh, mi võng mạc
        public string? VeinOcclusionType { get; set; } // trung tâm, nhánh
        public string? OcclusionType { get; set; } // phù, thiếu máu, hỗn hợp
        public bool OcclusionEdema { get; set; } = false;
        public bool OcclusionIschemia { get; set; } = false;

        // ===== RETINA (Võng mạc) =====
        public bool RetinaNormal { get; set; } = true;
        public string? RetinalCondition { get; set; } // Bình thường, Viêm mao mạch, Tân mạch võng mạc

        // Retinal findings
        public bool RetinaHemorrhageSuperficial { get; set; } = false;
        public bool RetinaHemorrhageDeep { get; set; } = false;
        public bool RetinaExudateHard { get; set; } = false;
        public bool RetinaExudateCottonWool { get; set; } = false;
        public bool RetinaEdema { get; set; } = false;
        public bool RetinaDegenerationPeripheral { get; set; } = false;
        public bool RetinaDegenerationCentral { get; set; } = false;
        public string? DegenerativeType { get; set; } // chu biên, trung tâm
        public string? DegenerativeDescription { get; set; }
        public bool RetinalDetachment { get; set; } = false;
        public string? RetinalDetachmentLevel { get; set; } // Mức độ
        public bool RetinalTear { get; set; } = false;
        public int? RetinalTearCount { get; set; }
        public string? RetinalTearLocation { get; set; } // Vị trí vết rách
        public string? RetinalTearMorphology { get; set; } // Hình thái

        // Retinal hemorrhage types
        public bool ChoroidalNeovascularization { get; set; } = false;
        public string? HemorrhageLocation { get; set; } // VM nông, VM sâu, Hắc mạc
        public string? ExudateType { get; set; } // Cứng, Dạng bông

        // ===== CHORIORETINITIS (Ổ viêm hắc mạc) - MS23 =====
        public bool ChorioretinitisActive { get; set; } = false;
        public bool ChorioretinitisScar { get; set; } = false;
        public int? ChorioretinitisCount { get; set; }
        public string? ChorioretinitisLocation { get; set; } // Vị trí

        // ===== INTRAOCULAR FOREIGN BODY (Dị vật nội nhãn) - MS21 =====
        public bool IntraocularForeignBody { get; set; } = false;
        public string? IofbLocation { get; set; }
        public string? IofbSize { get; set; }

        // ===== SUPRACHOROIDAL (Hắc mạc) - MS24 =====
        public bool ChoroidalNeovesselsSubretinal { get; set; } = false; // Tân mạch hắc mạc dưới Đĩa thị

        public string? RetinaVesselExtras { get; set; } // JSONB

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
