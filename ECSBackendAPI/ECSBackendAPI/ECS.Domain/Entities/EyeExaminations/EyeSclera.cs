using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.EyeExaminations
{
    /// <summary>
    /// Sclera examination.
    /// Covers MS21 (Trauma), MS24 (Glaucoma)
    /// </summary>
    public class EyeSclera : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public EyeSide Side { get; set; }

        // Basic
        public bool ScleraNormal { get; set; } = true;

        // Sclera conditions
        public bool ScleraEctasia { get; set; } = false;
        public bool ScleraThinning { get; set; } = false;
        public bool ScleraNecrosis { get; set; } = false;

        // Inflammation
        public bool Episcleritis { get; set; } = false;
        public string? ScleritisType { get; set; }

        // Laceration (Rách củng mạc) - MS21
        public bool ScleraLaceration { get; set; } = false;
        public bool? ScleraLacerationSutured { get; set; }
        public string? ScleraLacerationLocation { get; set; }
        public string? ScleraLacerationSize { get; set; }
        public bool ScleraTissueEntrapped { get; set; } = false; // Kẹt tổ chức

        // Scar
        public bool OldSurgeryScar { get; set; } = false;

        public string? ScleraExtras { get; set; } // JSONB

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
