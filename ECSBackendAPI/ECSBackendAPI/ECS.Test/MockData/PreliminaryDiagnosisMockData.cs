using ECS.Application.Services.MedicalRecordsServices.PreliminaryDiagnosisServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Test data factory for <see cref="PreliminaryDiagnosisService"/> tests.
    /// NOTE: <c>PreliminaryDiagnosis</c> is assumed to live in
    /// <c>ECS.Domain.Entities.MedicalRecords</c> (it is used without an extra using directive
    /// beyond the ones already imported by the service file). Adjust the namespace import above
    /// if the real type lives elsewhere.
    /// </summary>
    public static class PreliminaryDiagnosisMockData
    {
        public static readonly Guid AppointmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid PatientProfileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid DoctorProfileId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid DoctorUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");

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
                FullName = fullName
            };

        public static Appointment GetAppointment(
            Guid? id = null,
            Guid? patientId = null,
            AppointmentStatus status = AppointmentStatus.CONFIRMED) => new()
            {
                Id = id ?? AppointmentId,
                PatientId = patientId ?? PatientProfileId,
                AppointmentDate = DateTime.UtcNow.Date,
                Status = status,
                UpdatedAt = DateTime.UtcNow
            };

        public static PreliminaryDiagnosis GetExistingPreliminaryDiagnosis(
            Guid? appointmentId = null,
            Guid? patientId = null,
            Guid? doctorId = null) => new()
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId ?? AppointmentId,
                PatientId = patientId ?? PatientProfileId,
                DoctorId = doctorId ?? DoctorProfileId,
                UrgencyLevel = TriageUrgencyLevel.Medium,
                CheckInTime = DateTime.UtcNow,
                TriageCompletedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

        public static PreliminaryDiagnosisRequest GetValidRequest(
            Guid? appointmentId = null,
            TriageUrgencyLevel urgencyLevel = TriageUrgencyLevel.Medium,
            int? painLevel = null,
            string? recommendedAction = "Khám chuyên khoa mắt",
            DateTime? checkInTime = null) => new()
            {
                AppointmentId = (appointmentId ?? AppointmentId).ToString(),
                UrgencyLevel = urgencyLevel,
                PainLevel = painLevel,
                QuickVisualAssessment = "Thị lực giảm nhẹ",
                HasVisionChange = true,
                HasEyeRedness = false,
                HasEyeDischarge = false,
                HasLightSensitivity = false,
                HasEyePain = true,
                HasHeadache = false,
                HasForeignBody = false,
                RecommendedAction = recommendedAction,
                IsReferralNeeded = false,
                ReferralTo = null,
                FollowUpInstructions = "Tái khám sau 3 ngày",
                CheckInTime = checkInTime
            };
    }
}