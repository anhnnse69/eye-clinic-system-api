using ECS.Domain.Enums;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDetailServices
{
    /// <summary>
    /// Represents a summary of a medical record.
    /// </summary>
    public class MedicalRecordSummary
    {
        public Guid Id { get; set; }
        public RecordType RecordType { get; set; }
        public string? ChiefComplaint { get; set; }
        public string? DiagnosisMain { get; set; }
        public string? DiagnosisComorbid { get; set; }
        public string? TreatmentPlan { get; set; }
        public string? Notes { get; set; }
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<PrescriptionSummary> Prescriptions { get; set; } = [];
    }
}
