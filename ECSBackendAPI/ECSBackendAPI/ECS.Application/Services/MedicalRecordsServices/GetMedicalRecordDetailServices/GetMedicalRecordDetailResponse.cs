using System.Text.Json;

namespace ECS.Application.Services.MedicalRecordsServices.GetMedicalRecordDetailServices
{
    /// <summary>
    /// Response object for medical record detail.
    /// **Refactored (2026-07-14)**: complex clinical form data is now returned as
    /// a single <see cref="FormData"/> JSON element fetched from Cloudinary.
    /// </summary>
    public class GetMedicalRecordDetailResponse
    {
        public Guid Id { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public string RecordType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ChiefComplaint { get; set; }
        public string? Summary { get; set; }
        public string? Notes { get; set; }
        public bool IsLocked { get; set; }
        public DateTime? FinalizedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // ===== PATIENT =====
        public string PatientFullName { get; set; } = string.Empty;
        public string? PatientDob { get; set; }
        public string? PatientPhone { get; set; }
        public string? PatientEmail { get; set; }
        public string? PatientGender { get; set; }
        public string? PatientAddress { get; set; }
        public string? PatientIdentityNumber { get; set; }

        // ===== DOCTOR =====
        public string DoctorFullName { get; set; } = string.Empty;
        public string? DoctorTitle { get; set; }
        public string? DoctorSpecialty { get; set; }

        // ===== APPOINTMENT =====
        public DateTime AppointmentDate { get; set; }
        public string? AppointmentStatus { get; set; }
        public string? AppointmentNotes { get; set; }

        // ===== MONGODB PAYLOAD (form data) =====
        /// <summary>
        /// Full medical record form as JSON (Bệnh Án + Khám bệnh).
        /// Fetched from MongoDB on demand; structure depends on <see cref="RecordType"/>.
        /// </summary>
        public JsonElement? FormData { get; set; }

        /// <summary>
        /// MongoDB document ID that stores the full form JSON (24-char hex).
        /// </summary>
        public string? MongoDocumentId { get; set; }

        public string RecordDataSchemaVersion { get; set; } = "1.0";
        public int RecordDataVersion { get; set; }
        public long RecordDataSizeBytes { get; set; }
        public bool IntegrityValid { get; set; } = true;

        // ===== PERMISSIONS =====
        public bool CanEdit { get; set; }
        public bool CanViewOnly { get; set; }
        public string? EditRestrictionReason { get; set; }
    }
}