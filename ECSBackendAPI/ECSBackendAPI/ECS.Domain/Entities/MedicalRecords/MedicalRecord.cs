using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.EyeExaminations;
using ECS.Domain.Entities.General;
using ECS.Domain.Entities.Paraclinical;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Prescriptions;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Entities.SubspecialtyRecords;

namespace ECS.Domain.Entities.MedicalRecords
{
    public class MedicalRecord : EntityBase<Guid>
    {
        public Guid AppointmentId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public string? RecordType { get; set; }
        public string? ChiefComplaint { get; set; }
        public int? IllnessDayNumber { get; set; }
        public string? MedicalHistory { get; set; }
        public string? PersonalHistoryEye { get; set; }
        public string? PersonalHistorySystemic { get; set; }
        public string? FamilyHistory { get; set; }
        public int? VitalPulse { get; set; }
        public decimal? VitalTemperature { get; set; }
        public string? VitalBloodPressure { get; set; }
        public int? VitalRespiratoryRate { get; set; }
        public decimal? VitalWeightKg { get; set; }
        public bool SystemicEndocrineNormal { get; set; } = true;
        public string? SystemicEndocrineNote { get; set; }
        public bool SystemicNeuroNormal { get; set; } = true;
        public string? SystemicNeuroNote { get; set; }
        public bool SystemicCardioNormal { get; set; } = true;
        public string? SystemicCardioNote { get; set; }
        public bool SystemicRespiratoryNormal { get; set; } = true;
        public string? SystemicRespiratoryNote { get; set; }
        public bool SystemicDigestiveNormal { get; set; } = true;
        public string? SystemicDigestiveNote { get; set; }
        public bool SystemicMusculoNormal { get; set; } = true;
        public string? SystemicMusculoNote { get; set; }
        public bool SystemicUrogenitalNormal { get; set; } = true;
        public string? SystemicUrogenitalNote { get; set; }
        public string? SystemicOtherNote { get; set; }
        public string? Summary { get; set; }
        public string? DiagnosisMain { get; set; }
        public string? DiagnosisComorbid { get; set; }
        public string? DiagnosisDifferential { get; set; }
        public string? Prognosis { get; set; }
        public string? TreatmentPlan { get; set; }
        public string? Notes { get; set; }
        public bool IsLocked { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }

        public virtual Appointment Appointment { get; set; } = null!;
        public virtual PatientProfile Patient { get; set; } = null!;
        public virtual DoctorProfile Doctor { get; set; } = null!;
        public virtual ICollection<DocumentAccessPermission>? DocumentAccessPermissions { get; set; }
        public virtual ICollection<EmrExportLog>? EmrExportLogs { get; set; }
        public virtual EyeExamination? EyeExamination { get; set; }
        public virtual ICollection<OctResult>? OctResults { get; set; }
        public virtual ICollection<VisualFieldTest>? VisualFieldTests { get; set; }
        public virtual ICollection<UltrasoundEye>? UltrasoundEyes { get; set; }
        public virtual TraumaRecord? TraumaRecord { get; set; }
        public virtual GlaucomaRecord? GlaucomaRecord { get; set; }
        public virtual StrabismusPtosisRecord? StrabismusPtosisRecord { get; set; }
        public virtual PediatricEyeRecord? PediatricEyeRecord { get; set; }
        public virtual ICollection<Prescription>? Prescriptions { get; set; }
        public virtual ICollection<GlassesPrescription>? GlassesPrescriptions { get; set; }
    }
}
