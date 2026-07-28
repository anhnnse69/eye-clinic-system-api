using System.Text.Json;
using ECS.Application.Services.MedicalRecordsServices.CreateMedicalRecordServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Test data factory for <see cref="CreateMedicalRecordService"/> tests.
    /// NOTE: property names for Appointment / PatientProfile / MedicalRecord / Queue
    /// are inferred from usage inside CreateMedicalRecordService.cs. Adjust if the
    /// real entity shapes differ.
    /// </summary>
    public static class CreateMedicalRecordMockData
    {
        public static readonly Guid AppointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static readonly Guid PatientId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public static readonly Guid DoctorProfileId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public static readonly Guid DoctorUserId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        public static readonly Guid OtherUserId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        public const string ValidFormDataJson = """
        {
            "benhAn": {
                "lyDoVaoVien": "Đau mắt đỏ 3 ngày",
                "summary": null
            },
            "khamBenh": {
                "thiLuc": "10/10"
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
            var doctorUser = user ?? GetDoctorUser();
            return new DoctorProfile
            {
                Id = id ?? DoctorProfileId,
                UserId = userId ?? doctorUser.Id,
                IsActive = isActive,
                User = doctorUser
            };
        }

        public static Appointment GetAppointment(
            Guid? id = null,
            Guid? patientId = null,
            PatientProfile? patient = null,
            DoctorProfile? doctor = null,
            AppointmentStatus status = AppointmentStatus.CONFIRMED)
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
            Guid? appointmentId = null,
            Guid? patientId = null,
            Guid? doctorId = null) => new()
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId ?? AppointmentId,
                PatientId = patientId ?? PatientId,
                DoctorId = doctorId ?? DoctorProfileId,
                RecordType = RecordType.MS21_TRAUMA,
                Status = RecordStatus.DRAFT,
                IsLocked = false,
                MongoDocumentId = "existing-mongo-id",
                RecordDataSchemaVersion = "1.0",
                RecordDataVersion = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

        public static CreateMedicalRecordRequest GetValidRequest(
            Guid? appointmentId = null,
            Guid? patientId = null,
            string recordType = "MS21_TRAUMA",
            string? notes = "Ghi chú của bác sĩ",
            string formDataJson = ValidFormDataJson)
        {
            return new CreateMedicalRecordRequest
            {
                AppointmentId = (appointmentId ?? AppointmentId).ToString(),
                PatientId = (patientId ?? PatientId).ToString(),
                RecordType = recordType,
                Notes = notes,
                FormData = BuildFormData(formDataJson)
            };
        }
    }
}