using System.Text.Json;
using ECS.Application.Services.MedicalRecordsServices.UpdateMedicalRecordServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Test data factory for <see cref="UpdateMedicalRecordService"/> tests.
    /// </summary>
    public static class UpdateMedicalRecordMockData
    {
        public static readonly Guid RecordId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid AppointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static readonly Guid PatientId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public static readonly Guid DoctorProfileId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public static readonly Guid DoctorUserId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        public static readonly Guid OtherDoctorProfileId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        public static readonly Guid OtherUserId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        public const string ValidFormDataJson = """
        {
            "benhAn": {
                "lyDoVaoVien": "Cập nhật: Đau mắt đỏ 5 ngày",
                "summary": null
            },
            "khamBenh": {
                "thiLuc": "9/10"
            }
        }
        """;

        public static JsonElement BuildFormData(string json)
            => JsonDocument.Parse(json).RootElement.Clone();

        public static PatientProfile GetPatientProfile(
            Guid? id = null,
            string fullName = "Nguyen Van A") => new()
            {
                Id = id ?? PatientId,
                FullName = fullName
            };

        public static User GetDoctorUser(
            Guid? id = null,
            string fullName = "Doctor A") => new()
            {
                Id = id ?? DoctorUserId,
                Phone = "0901111111",
                Email = "doctor.a@example.com",
                PasswordHash = "x",
                FullName = fullName,
                Role = UserRole.DOCTOR,
                IsActive = true
            };

        public static DoctorProfile GetDoctorProfile(
            Guid? id = null,
            Guid? userId = null,
            User? user = null,
            bool isActive = true)
        {
            var doctorUser = user ?? GetDoctorUser(userId);
            return new DoctorProfile
            {
                Id = id ?? DoctorProfileId,
                UserId = doctorUser.Id,
                IsActive = isActive,
                User = doctorUser
            };
        }

        public static Appointment GetAppointment(
            Guid? id = null,
            Guid? patientId = null,
            PatientProfile? patient = null,
            DoctorProfile? doctor = null,
            AppointmentStatus status = AppointmentStatus.IN_PROGRESS)
        {
            var patientProfile = patient ?? GetPatientProfile(patientId ?? PatientId);
            return new Appointment
            {
                Id = id ?? AppointmentId,
                PatientId = patientProfile.Id,
                Patient = patientProfile,
                Doctor = doctor ?? GetDoctorProfile(),
                AppointmentDate = DateTime.UtcNow.Date,
                Status = status,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static MedicalRecord GetExistingMedicalRecord(
            Guid? id = null,
            Guid? appointmentId = null,
            Guid? patientId = null,
            Guid? doctorId = null,
            bool isLocked = false,
            RecordType recordType = RecordType.MS21_TRAUMA,
            Appointment? appointment = null,
            DoctorProfile? doctor = null)
        {
            var doc = doctor ?? GetDoctorProfile(doctorId);
            var app = appointment ?? GetAppointment(appointmentId, patientId, doctor: doc);

            return new MedicalRecord
            {
                Id = id ?? RecordId,
                AppointmentId = app.Id,
                Appointment = app,
                PatientId = app.PatientId,
                DoctorId = doc.Id,
                Doctor = doc,
                RecordType = recordType,
                Status = RecordStatus.DRAFT,
                IsLocked = isLocked,
                MongoDocumentId = "existing-mongo-id",
                RecordDataSchemaVersion = "1.0",
                RecordDataVersion = 1,
                RecordDataChecksum = "old-checksum",
                RecordDataSizeBytes = 100,
                Notes = "Ghi chú cũ",
                ChiefComplaint = "Cũ",
                Summary = "Cũ",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                UpdatedAt = DateTime.UtcNow.AddHours(-1)
            };
        }

        public static UpdateMedicalRecordRequest GetValidRequest(
            string? notes = "Ghi chú cập nhật từ bác sĩ",
            string formDataJson = ValidFormDataJson,
            string editReason = "Bổ sung diễn biến lâm sàng và đính chính chẩn đoán ban đầu theo kết quả xét nghiệm mới.",
            string editPermissionDocument = "GP-2026-0818/QĐ-CA")
        {
            return new UpdateMedicalRecordRequest
            {
                Notes = notes,
                EditReason = editReason,
                EditPermissionDocument = editPermissionDocument,
                FormData = BuildFormData(formDataJson)
            };
        }
    }
}