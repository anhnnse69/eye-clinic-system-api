using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;

namespace ECS.Domain.Entities.SubspecialtyRecords
{
    /// <summary>
    /// Strabismus & Ptosis record - MS25.
    /// </summary>
    public class StrabismusPtosisRecord : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }

        // Chief complaint & cause
        public bool ChiefStrabismus { get; set; } = false;
        public bool ChiefPtosis { get; set; } = false;
        public bool Congenital { get; set; } = false;
        public bool Acquired { get; set; } = false;
        public string? AcquiredOnset { get; set; }

        // Prior treatment history
        public string? PriorAmblyopiaTreatment { get; set; }
        public string? PriorSurgery { get; set; }

        // Symptoms (JSONB)
        public string? StrabismusType { get; set; } // esotropia, exotropia, vertical
        public bool Nystagmus { get; set; } = false;
        public string? NystagmusType { get; set; }

        // Refraction after atropine (JSONB)
        public string? RefractionPreAtropine { get; set; }
        public string? RefractionPostAtropine { get; set; }

        // Cover test & prism measurements
        public string? CoverTestResult { get; set; }
        public string? PrismMeasurements { get; set; } // {od: {near, distance, up, down}, os: {...}}
        public string? StrabismusSyndrome { get; set; }

        // Binocular vision (JSONB)
        public string? BinocularStatus { get; set; } // simultaneous_vision, fusion, stereopsis, diplopia, head_posture

        // Ptosis measurements
        public string? PtosisOdDegree { get; set; }
        public string? PtosisOsDegree { get; set; }
        public string? LevatorFunctionOd { get; set; }
        public string? LevatorFunctionOs { get; set; }
        public string? MarcusGunn { get; set; } // JSONB
        public string? FixationOd { get; set; }
        public string? FixationOs { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
