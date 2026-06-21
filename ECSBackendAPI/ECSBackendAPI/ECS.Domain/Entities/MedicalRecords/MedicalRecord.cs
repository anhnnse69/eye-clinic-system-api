using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.EyeExaminations;
using ECS.Domain.Entities.General;
using ECS.Domain.Entities.Paraclinical;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Prescriptions;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Entities.SubspecialtyRecords;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.MedicalRecords
{
    /// <summary>
    /// Core medical record for an eye examination visit.
    /// </summary>
    public class MedicalRecord : EntityBase<Guid>
    {
        public Guid AppointmentId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public RecordType RecordType { get; set; }

        // Chief complaint & history
        public string? ChiefComplaint { get; set; }
        public int? IllnessDayNumber { get; set; }
        public string? MedicalHistory { get; set; }
        public string? PersonalHistoryEye { get; set; }
        public string? PersonalHistorySystemic { get; set; }
        public string? FamilyHistory { get; set; }

        // Vital signs
        public int? VitalPulse { get; set; }
        public decimal? VitalTemperature { get; set; }
        public string? VitalBloodPressure { get; set; }
        public int? VitalRespiratoryRate { get; set; }
        public decimal? VitalWeightKg { get; set; }

        // Systemic examination (JSONB - endocrine, neuro, cardio, respiratory, digestive, musculo, urogenital)
        public string? SystemicExam { get; set; }

        // Diagnosis
        public string? DiagnosisMain { get; set; }
        public string? DiagnosisComorbid { get; set; }
        public string? DiagnosisDifferential { get; set; }
        public string? Prognosis { get; set; }
        public string? TreatmentPlan { get; set; }

        public string? Notes { get; set; }
        public bool IsLocked { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public virtual Appointment Appointment { get; set; } = null!;
        public virtual PatientProfile Patient { get; set; } = null!;
        public virtual DoctorProfile Doctor { get; set; } = null!;

        public virtual ICollection<DocumentAccessPermission>? DocumentAccessPermissions { get; set; }
        public virtual ICollection<EmrExportLog>? EmrExportLogs { get; set; }

        // Eye examination modules
        public virtual ICollection<EyeExamBasic>? EyeExamBasics { get; set; }
        public virtual ICollection<EyeEyelidConjunctiva>? EyeEyelidConjunctivae { get; set; }
        public virtual ICollection<EyeCornea>? EyeCorneas { get; set; }
        public virtual ICollection<EyeAcIris>? EyeAcIrises { get; set; }
        public virtual ICollection<EyeLensVitreous>? EyeLensVitreouses { get; set; }
        public virtual ICollection<EyeSclera>? EyeScleras { get; set; }
        public virtual ICollection<EyeFundusDiscMacula>? EyeFundusDiscMaculas { get; set; }
        public virtual ICollection<EyeFundusRetinaVessel>? EyeFundusRetinaVessels { get; set; }
        public virtual ICollection<LacrimalRecord>? LacrimalRecords { get; set; }

        // Extras
        public virtual MedicalRecordExtras? Extras { get; set; }

        // Paraclinical
        public virtual ICollection<OctResult>? OctResults { get; set; }
        public virtual ICollection<VisualFieldTest>? VisualFieldTests { get; set; }
        public virtual ICollection<UltrasoundEye>? UltrasoundEyes { get; set; }

        // Subspecialty records
        public virtual TraumaRecord? TraumaRecord { get; set; }
        public virtual ICollection<TraumaSurgery>? TraumaSurgeries { get; set; }
        public virtual GlaucomaRecord? GlaucomaRecord { get; set; }
        public virtual StrabismusPtosisRecord? StrabismusPtosisRecord { get; set; }
        public virtual PediatricEyeRecord? PediatricRecord { get; set; }

        // Prescriptions
        public virtual ICollection<Prescription>? Prescriptions { get; set; }
        public virtual ICollection<GlassesPrescription>? GlassesPrescriptions { get; set; }
    }
}
