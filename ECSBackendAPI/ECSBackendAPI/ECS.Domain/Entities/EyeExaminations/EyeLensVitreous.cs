using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.EyeExaminations
{
    /// <summary>
    /// Lens & Vitreous examination.
    /// Covers MS21, MS22, MS23, MS24, MS25, MS26
    /// </summary>
    public class EyeLensVitreous : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public EyeSide Side { get; set; }

        // ===== LENS (Thể thủy tinh) =====
        public bool LensClear { get; set; } = true;
        public string? LensOpacityType { get; set; }
        public string? LensOpacityLocation { get; set; } // Nhân, Vỏ, Dưới bao, Toàn bộ
        public bool LensRupture { get; set; } = false;
        public bool LensSubluxation { get; set; } = false; // Sa lệch
        public bool LensIntoAnterior { get; set; } = false; // Ra tiền phòng
        public bool LensIntoVitreous { get; set; } = false; // Vào buồng dịch kính - MS21
        public bool LensIolPresent { get; set; } = false;
        public string? LensIolStatus { get; set; }
        public string? LensIolPosition { get; set; } // Cân, Lệch - MS22, MS24
        public bool LensAnteriorPigmentation { get; set; } = false; // Dính sắc tố mặt trước thể thủy tinh - MS23
        public bool LensPurulent { get; set; } = false; // Viêm mủ - MS21

        // ===== VITREOUS (Dịch kính) =====
        public bool VitreousClear { get; set; } = true;
        public bool VitreousOpacity { get; set; } = false;
        public bool VitreousHemorrhage { get; set; } = false;
        public bool VitreousPvd { get; set; } = false; // Bong dịch kính sau
        public string? VitreousTyndall { get; set; }
        public bool VitreousOrganized { get; set; } = false; // Tổ chức hóa - MS21
        public bool VitreousPurulent { get; set; } = false; // Viêm mủ - MS21
        public bool VitreousForeignBody { get; set; } = false; // Dị vật - MS21

        public string? LensVitreousExtras { get; set; } // JSONB

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
