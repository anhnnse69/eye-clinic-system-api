using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class ViewPatientDetailMockData
    {
        public static readonly Guid DefaultDoctorUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid DefaultDoctorProfileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid DefaultPatientId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid DefaultPatientUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid DefaultAppointmentId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        public static readonly Guid DefaultMedicalRecordId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        public static readonly Guid DefaultServiceId = Guid.Parse("77777777-7777-7777-7777-777777777777");

        public static DoctorProfile GetDoctorProfile(
            Guid? id = null,
            Guid? userId = null,
            bool isActive = true) => new()
        {
            Id = id ?? DefaultDoctorProfileId,
            UserId = userId ?? DefaultDoctorUserId,
            IsActive = isActive
        };

        public static User GetUser(Guid? id = null, string? avatarUrl = "https://example.com/avatar.jpg") => new()
        {
            Id = id ?? DefaultPatientUserId,
            Phone = "0987654321",
            Email = "patient@example.com",
            PasswordHash = "x",
            FullName = "Patient User",
            Role = UserRole.PATIENT,
            IsActive = true,
            AvatarUrl = avatarUrl
        };

        public static PatientProfile GetPatientProfile(
            Guid? id = null,
            User? user = null,
            string? avatarUrl = null) => new()
        {
            Id = id ?? DefaultPatientId,
            UserId = (user ?? GetUser(avatarUrl: avatarUrl)).Id,
            User = user ?? GetUser(avatarUrl: avatarUrl),
            FullName = "Nguyen Van A",
            Gender = Gender.MALE,
            Dob = new DateTime(1990, 1, 1),
            IdentityNumber = "012345678901",
            Address = "123 Le Loi, Quan 1, TP.HCM",
            PhoneNumber = "0987654321",
            BhytNumber = "DN4010123456789",
            BloodType = "O+",
            Allergies = "Penicillin",
            MedicalHistory = "Hypertension"
        };

        public static Service GetService(Guid? id = null) => new()
        {
            Id = id ?? DefaultServiceId,
            ClinicId = Guid.NewGuid(),
            ServiceName = "Eye Examination",
            Price = 200_000m,
            DurationMinutes = 30,
            IsActive = true
        };

        public static MedicalRecord GetMedicalRecord(Guid? id = null, RecordType recordType = RecordType.MS21_TRAUMA) => new()
        {
            Id = id ?? DefaultMedicalRecordId,
            AppointmentId = Guid.NewGuid(),
            PatientId = DefaultPatientId,
            DoctorId = DefaultDoctorProfileId,
            RecordType = recordType,
            ChiefComplaint = "Blurred vision in left eye",
            Summary = "Mild cataract diagnosed",
            Notes = "Schedule follow-up in 3 months",
            Status = RecordStatus.FINALIZED,
            IsLocked = true,
            CreatedAt = new DateTime(2026, 1, 15),
            RecordDataUrl = "https://cloudinary.example/record.json",
            RecordDataPublicId = "emr/record123",
            RecordDataSchemaVersion = "1.0",
            RecordDataVersion = 1,
            RecordDataSizeBytes = 4096,
            RecordDataChecksum = "sha256:abc"
        };

        public static Appointment GetAppointment(
            Guid? id = null,
            Guid? doctorId = null,
            Guid? patientId = null,
            Service? service = null,
            MedicalRecord? medicalRecord = null,
            DateTime? appointmentDate = null,
            AppointmentStatus status = AppointmentStatus.COMPLETED,
            string? symptoms = "Blurred vision")
        {
            var patientProfile = GetPatientProfile();
            return new Appointment
            {
                Id = id ?? DefaultAppointmentId,
                DoctorId = doctorId ?? DefaultDoctorProfileId,
                PatientId = patientId ?? patientProfile.Id,
                SlotId = Guid.NewGuid(),
                ServiceId = (service ?? GetService()).Id,
                AppointmentDate = appointmentDate ?? new DateTime(2026, 1, 15, 9, 0, 0),
                Status = status,
                Symptoms = symptoms,
                Service = service ?? GetService(),
                MedicalRecord = medicalRecord,
                Patient = patientProfile,
                Doctor = GetDoctorProfile()
            };
        }
    }
}