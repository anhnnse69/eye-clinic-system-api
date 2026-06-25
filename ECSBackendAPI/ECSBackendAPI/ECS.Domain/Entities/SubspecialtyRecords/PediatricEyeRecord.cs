using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;

namespace ECS.Domain.Entities.SubspecialtyRecords
{
    /// <summary>
    /// Pediatric eye record - MS26 (Mắt trẻ em).
    /// Contains comprehensive pediatric eye examination data.
    /// </summary>
    public class PediatricEyeRecord : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }

        // ===== HISTORY =====
        public bool Congenital { get; set; } = false;
        public bool Acquired { get; set; } = false;
        public string? AcquiredOnset { get; set; }
        public string? PriorTreatment { get; set; }

        // Pregnancy & development
        public bool PregnancyIllness { get; set; } = false;
        public string? PregnancyIllnessDetail { get; set; }
        public bool IntellectualDevelopmentNormal { get; set; } = true;

        // MS26 Specific History Fields
        /// <summary>
        /// Pathological pregnancy history.
        /// </summary>
        public string? PediatricPregnancyHistory { get; set; }
        /// <summary>
        /// Intellectual development status.
        /// </summary>
        public string? PediatricDevelopment { get; set; }

        // Chief symptoms (JSONB)
        public string? ChiefSymptoms { get; set; }

        // ===== EYELID CONDITIONS =====
        public bool EntropionOd { get; set; } = false;
        public bool EpicanthusOd { get; set; } = false;
        public bool PtosisOd { get; set; } = false;
        public string? EyelidTumor { get; set; }
        public string? EyelidTumorLocation { get; set; }
        public string? EyelidTumorSize { get; set; }

        // ===== EYEBALL STATUS =====
        public string? EyeballOdStatus { get; set; }
        public string? EyeballOsStatus { get; set; }
        public string? EyeballTexture { get; set; } // Mềm, Căng, To, Nhỏ, Teo

        // ===== AMBLYOPIA (Nhược thị) =====
        public string? AmblyopiaStatus { get; set; }
        public string? FixationPreferenceOd { get; set; } // Trung tâm, Cạnh tâm, Ngoại tâm
        public string? FixationPreferenceOs { get; set; }

        // ===== FUNDUS SUMMARY =====
        public string? FundusSummaryOd { get; set; }
        public string? FundusSummaryOs { get; set; }

        // ===== DEVELOPMENTAL STATUS =====
        public string? IntellectualDevelopmentStatus { get; set; }
        public string? GeneralHealthStatus { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
