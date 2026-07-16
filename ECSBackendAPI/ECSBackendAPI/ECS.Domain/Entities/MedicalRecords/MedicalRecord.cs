using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.General;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.MedicalRecords
{
    /// <summary>
    /// Core medical record for an eye examination visit.
    /// **Refactored (2026-07-14)**: complex examination form data (50+ fields,
    /// 18+ nav properties) is now stored as JSON on Cloudinary (raw resource).
    /// DB only keeps metadata + RecordDataUrl pointing to the JSON file.
    /// See <c>DOC/docs/architecture/MEDICAL_RECORD_CLOUDINARY_DESIGN.md</c>.
    /// </summary>
    public class MedicalRecord : EntityBase<Guid>
    {
        // ===== CORE RELATIONSHIPS =====
        public Guid AppointmentId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public RecordType RecordType { get; set; }

        // ===== METADATA (truy vấn được trên DB) =====
        /// <summary>
        /// Chief complaint (lý do vào viện) — short text, extracted from form JSON.
        /// </summary>
        public string? ChiefComplaint { get; set; }

        /// <summary>
        /// Short summary of the record — extracted from form JSON.
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// Doctor's free-form notes (separate from clinical form).
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Lifecycle status of the medical record.
        /// </summary>
        public RecordStatus Status { get; set; } = RecordStatus.DRAFT;

        public bool IsLocked { get; set; } = false;

        /// <summary>
        /// When the record was finalized (chốt bệnh án).
        /// </summary>
        public DateTime? FinalizedAt { get; set; }

        /// <summary>
        /// User who finalized the record.
        /// </summary>
        public Guid? FinalizedBy { get; set; }

        // ===== CLOUD STORAGE (single source of truth for form data) =====

        /// <summary>
        /// MongoDB ObjectId (24-char hex) of the document storing the full form JSON.
        /// Replaces the Cloudinary JSON-envelope design (2026-07-15 refactor).
        /// </summary>
        public string? MongoDocumentId { get; set; }

        // ===== CLOUD STORAGE (kept for backwards compatibility / legacy fallback) =====

        /// <summary>
        /// Legacy Cloudinary signed URL — kept as a fallback pointer when
        /// <see cref="MongoDocumentId"/> is null (records written before the
        /// 2026-07-15 migration). Will be removed in a future release.
        /// </summary>
        public string RecordDataUrl { get; set; } = string.Empty;

        /// <summary>
        /// Legacy Cloudinary public_id — see <see cref="RecordDataUrl"/>.
        /// </summary>
        public string RecordDataPublicId { get; set; } = string.Empty;

        /// <summary>
        /// Schema version of the JSON payload (semver-like, e.g. "1.0").
        /// </summary>
        public string RecordDataSchemaVersion { get; set; } = "1.0";

        /// <summary>
        /// Increments on every update — for optimistic concurrency control.
        /// </summary>
        public int RecordDataVersion { get; set; } = 1;

        /// <summary>
        /// Size of the JSON file in bytes.
        /// </summary>
        public long RecordDataSizeBytes { get; set; }

        /// <summary>
        /// SHA-256 checksum of the JSON content (integrity check).
        /// </summary>
        public string RecordDataChecksum { get; set; } = string.Empty;

        // ===== AUDIT (EntityBase cung cấp CreatedAt, UpdatedAt, CreatedBy, UpdatedBy) =====
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }

        // ===== NAVIGATION =====
        public virtual Appointment Appointment { get; set; } = null!;
        public virtual PatientProfile Patient { get; set; } = null!;
        public virtual DoctorProfile Doctor { get; set; } = null!;
        public virtual User? FinalizedByUser { get; set; }

        // ===== OPTIONAL: Pre-existing access logs (giữ lại vì không thuộc form data) =====
        public virtual ICollection<DocumentAccessPermission>? DocumentAccessPermissions { get; set; }
        public virtual ICollection<EmrExportLog>? EmrExportLogs { get; set; }
    }
}