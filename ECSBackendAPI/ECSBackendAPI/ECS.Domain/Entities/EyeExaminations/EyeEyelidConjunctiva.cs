using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.EyeExaminations
{
    /// <summary>
    /// Eyelid & Conjunctiva examination.
    /// </summary>
    public class EyeEyelidConjunctiva : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public EyeSide Side { get; set; }

        // Eyelid
        public bool EyelidEdema { get; set; } = false;
        public bool Ptosis { get; set; } = false;
        public string? PtosisDegree { get; set; }
        public bool Entropion { get; set; } = false;
        public bool Ectropion { get; set; } = false;
        public bool Lagophthalmos { get; set; } = false;
        public bool Laceration { get; set; } = false;
        public string? LacerationDepth { get; set; }
        public bool Scar { get; set; } = false;
        public bool Chalazion { get; set; } = false;
        public bool Hordeolum { get; set; } = false;
        public string? EyelidOther { get; set; } // JSONB

        // Conjunctiva
        public string? ConjunctivaCongestionType { get; set; }
        public bool ConjunctivaEdema { get; set; } = false;
        public bool ConjunctivaHemorrhage { get; set; } = false;
        public bool ConjunctivaPapilla { get; set; } = false;
        public bool ConjunctivaFollicle { get; set; } = false;
        public bool ConjunctivaKeratinization { get; set; } = false;
        public bool ConjunctivaScar { get; set; } = false;
        public string? ConjunctivaDischarge { get; set; }
        public bool FluoresceinStain { get; set; } = false;
        public string? ConjunctivaOther { get; set; } // JSONB

        // Fornix (for MS22)
        public string? FornixStatus { get; set; }
        public string? SymblepharonHeight { get; set; }
        public string? SymblepharonWidth { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
