using ECS.Application.Services.MedicalRecordsServices.GetMedicalRecordDetailServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Test data factory for <see cref="GetMedicalRecordDetailService"/> tests.
    /// NOTE: property names for PatientProfile (Dob, PhoneNumber, Gender, Address,
    /// IdentityNumber), DoctorProfile (Title, Specialty), and Appointment (NoteReason)
    /// are inferred from usage inside GetMedicalRecordDetailService.cs /
    /// GetMedicalRecordDetailResponse.cs. Adjust if the real entity shapes differ.
    /// </summary>
    public static class GetMedicalRecordDetailMockData
    {
        public static readonly Guid RecordId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid AppointmentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid PatientProfileId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid PatientUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid DoctorProfileId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        public static readonly Guid DoctorUserId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        public static readonly Guid OtherProfileId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        public static readonly Guid SpecialtyId = Guid.Parse("88888888-8888-8888-8888-888888888888");

        public const string ValidMongoDocumentId = "507f1f77bcf86cd799439011";
        public const string ValidFormDataJson = """{ "benhAn": { "lyDoVaoVien": "Đau mắt đỏ 3 ngày" } }""";

        public static User GetPatientUser(Guid? id = null, string? email = "patient@example.com") => new()
        {
            Id = id ?? PatientUserId,
            Phone = "0900000000",
            Email = email ?? string.Empty,
            PasswordHash = "x",
            FullName = "Nguyen Van A",
            Role = UserRole.PATIENT,
            IsActive = true
        };

        public static User GetDoctorUser(Guid? id = null, string fullName = "Doctor A") => new()
        {
            Id = id ?? DoctorUserId,
            Phone = "0901111111",
            Email = "doctor.a@example.com",
            PasswordHash = "x",
            FullName = fullName,
            Role = UserRole.DOCTOR,
            IsActive = true
        };

        public static Specialty GetSpecialty(string name = "Nhãn khoa") => new()
        {
            Id = SpecialtyId,
            Name = name
        };

        public static PatientProfile GetPatientProfile(
            Guid? id = null,
            Guid? userId = null,
            User? user = null) => new()
            {
                Id = id ?? PatientProfileId,
                UserId = userId ?? PatientUserId,
                User = user ?? GetPatientUser(),
                FullName = "Nguyen Van A",
                Dob = new DateTime(1990, 1, 1),
                PhoneNumber = "0900000000",
                Gender = Gender.MALE,
                Address = "123 Le Loi, Q1",
                IdentityNumber = "079090001234"
            };

        public static DoctorProfile GetDoctorProfile(
            Guid? id = null,
            Guid? userId = null,
            User? user = null,
            bool isActive = true,
            Specialty? specialty = null) => new()
            {
                Id = id ?? DoctorProfileId,
                UserId = userId ?? DoctorUserId,
                IsActive = isActive,
                User = user ?? GetDoctorUser(),
                Title = "BSCKI",
                Specialty = specialty ?? GetSpecialty()
            };

        public static Appointment GetAppointment(
            Guid? id = null,
            AppointmentStatus status = AppointmentStatus.CONFIRMED) => new()
            {
                Id = id ?? AppointmentId,
                AppointmentDate = new DateTime(2026, 7, 20),
                Status = status,
                NoteReason = "Tái khám định kỳ",
                UpdatedAt = DateTime.UtcNow
            };

        public static MedicalRecord GetMedicalRecord(
            Guid? id = null,
            Guid? patientId = null,
            Guid? doctorId = null,
            Guid? appointmentId = null,
            PatientProfile? patient = null,
            DoctorProfile? doctor = null,
            Appointment? appointment = null,
            bool isLocked = false,
            string? mongoDocumentId = ValidMongoDocumentId)
        {
            var patientProfile = patient ?? GetPatientProfile();
            var doctorProfile = doctor ?? GetDoctorProfile();
            return new MedicalRecord
            {
                Id = id ?? RecordId,
                AppointmentId = appointmentId ?? AppointmentId,
                PatientId = patientId ?? patientProfile.Id,
                DoctorId = doctorId ?? doctorProfile.Id,
                Patient = patientProfile,
                Doctor = doctorProfile,
                Appointment = appointment ?? GetAppointment(appointmentId ?? AppointmentId),
                RecordType = RecordType.MS21_TRAUMA,
                Status = RecordStatus.DRAFT,
                IsLocked = isLocked,
                MongoDocumentId = mongoDocumentId,
                RecordDataSchemaVersion = "1.0",
                RecordDataVersion = 1,
                RecordDataSizeBytes = 1024,
                ChiefComplaint = "Đau mắt đỏ",
                Summary = "Đau mắt đỏ 3 ngày",
                Notes = "Ghi chú bác sĩ",
                FinalizedAt = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static GetMedicalRecordDetailRequest GetValidRequest(Guid? id = null) => new()
        {
            Id = id ?? RecordId
        };
    }
}