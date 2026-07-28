using ECS.Application.Services.MedicalRecordsServices.GetMedicalRecordsServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Test data factory for <see cref="GetMedicalRecordsService"/> tests.
    /// NOTE: property names for PatientProfile (Dob, PhoneNumber) and Appointment
    /// (AppointmentDate) are inferred from usage inside GetMedicalRecordsService.cs.
    /// Adjust if the real entity shapes differ.
    /// </summary>
    public static class GetMedicalRecordsMockData
    {
        public static readonly Guid DoctorProfileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid DoctorUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid OtherDoctorProfileId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid OtherDoctorUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid PatientProfileId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        public static readonly Guid AppointmentId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        public static readonly Guid RecordId = Guid.Parse("77777777-7777-7777-7777-777777777777");

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

        public static DoctorProfile GetDoctorProfile(
            Guid? id = null,
            Guid? userId = null,
            User? user = null,
            bool isActive = true) => new()
            {
                Id = id ?? DoctorProfileId,
                UserId = userId ?? DoctorUserId,
                IsActive = isActive,
                User = user ?? GetDoctorUser(id: userId ?? DoctorUserId)
            };

        public static PatientProfile GetPatientProfile(
            Guid? id = null,
            string fullName = "Nguyen Van A") => new()
            {
                Id = id ?? PatientProfileId,
                FullName = fullName,
                Dob = new DateTime(1990, 1, 1),
                PhoneNumber = "0900000000"
            };

        public static Appointment GetAppointment(
            Guid? id = null,
            DateTime? appointmentDate = null) => new()
            {
                Id = id ?? AppointmentId,
                AppointmentDate = appointmentDate ?? DateTime.UtcNow.Date,
                Status = AppointmentStatus.CONFIRMED,
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
            RecordType recordType = RecordType.MS21_TRAUMA,
            string? chiefComplaint = "Đau mắt đỏ",
            string? summary = "Viêm kết mạc",
            string? notes = "Ghi chú bác sĩ",
            bool isLocked = false,
            DateTime? createdAt = null)
        {
            var patientProfile = patient ?? GetPatientProfile();
            var doctorProfile = doctor ?? GetDoctorProfile();
            var appt = appointment ?? GetAppointment(appointmentId ?? AppointmentId);
            return new MedicalRecord
            {
                Id = id ?? RecordId,
                AppointmentId = appointmentId ?? appt.Id,
                PatientId = patientId ?? patientProfile.Id,
                DoctorId = doctorId ?? doctorProfile.Id,
                Patient = patientProfile,
                Doctor = doctorProfile,
                Appointment = appt,
                RecordType = recordType,
                Status = RecordStatus.DRAFT,
                IsLocked = isLocked,
                MongoDocumentId = "507f1f77bcf86cd799439011",
                RecordDataSchemaVersion = "1.0",
                RecordDataVersion = 1,
                ChiefComplaint = chiefComplaint,
                Summary = summary,
                Notes = notes,
                CreatedAt = createdAt ?? DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static GetMedicalRecordsRequest GetValidRequest(
            int pageNumber = 1,
            int pageSize = 10,
            DateTime? startDate = null,
            DateTime? endDate = null,
            RecordType? recordType = null,
            Guid? doctorId = null,
            string? searchTerm = null) => new()
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                StartDate = startDate,
                EndDate = endDate,
                RecordType = recordType,
                DoctorId = doctorId,
                SearchTerm = searchTerm
            };
    }
}