using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;

namespace ECS.Domain.Entities.SubspecialtyRecords
{
    /// <summary>
    /// Glaucoma record - MS24.
    /// </summary>
    public class GlaucomaRecord : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }

        // Symptoms (JSONB)
        public string? Symptoms { get; set; } // eye_pain, blurred_vision, visual_field_constriction, halos, photophobia, tearing, red_eye, headache, nausea, vomiting

        // History (JSONB)
        public string? HistoryEye { get; set; } // myopia, hyperopia, trauma, uveitis, anterior_segment_inflammation, prior_surgery
        public string? HistorySteroid { get; set; } // use, drug_name, duration, route
        public string? HistorySystemic { get; set; } // cardiovascular, hypertension, diabetes, carotid_fistula
        public string? FamilyGlaucoma { get; set; } // grandparents, parents, siblings, other_relatives

        // Classification
        public string? GlaucomaType { get; set; }
        public decimal? IopTargetOd { get; set; }
        public decimal? IopTargetOs { get; set; }
        public string? StageOd { get; set; }
        public string? StageOs { get; set; }

        // Gonioscopy
        public string? GonioscopyOd { get; set; }
        public string? GonioscopyOs { get; set; }

        // Bleb
        public string? BlebOdStatus { get; set; }
        public string? BlebOsStatus { get; set; }

        // Optic disc
        public string? OpticDiscDescription { get; set; }
        public string? NerveRimOd { get; set; }
        public string? NerveRimOs { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
        public virtual ICollection<GlaucomaHistory>? Histories { get; set; }
    }
}
