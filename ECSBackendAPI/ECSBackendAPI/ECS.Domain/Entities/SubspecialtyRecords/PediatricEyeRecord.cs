using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;

namespace ECS.Domain.Entities.SubspecialtyRecords
{
    /// <summary>
    /// Pediatric eye record - MS26.
    /// </summary>
    public class PediatricEyeRecord : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }

        // Specific history
        public bool Congenital { get; set; } = false;
        public bool Acquired { get; set; } = false;
        public string? AcquiredOnset { get; set; }
        public string? PriorTreatment { get; set; }

        // Pregnancy & development
        public bool PregnancyIllness { get; set; } = false;
        public string? PregnancyIllnessDetail { get; set; }
        public bool IntellectualDevelopmentNormal { get; set; } = true;

        // Chief symptoms (JSONB)
        public string? ChiefSymptoms { get; set; } // blurred_vision, eye_pain, red_eye, photophobia

        // Pediatric eyelid conditions
        public bool EntropionOd { get; set; } = false;
        public bool EpicanthusOd { get; set; } = false;
        public bool PtosisOd { get; set; } = false;

        // Eyeball & amblyopia
        public string? EyeballOdStatus { get; set; }
        public string? AmblyopiaStatus { get; set; }

        // Fundus summary (difficult to examine in children)
        public string? FundusSummaryOd { get; set; }
        public string? FundusSummaryOs { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
