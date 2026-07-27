using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class ViewListPatientMockData
    {
        public static readonly Guid DefaultDoctorUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid DefaultDoctorProfileId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public static DoctorProfile GetDoctorProfile(Guid? id = null, Guid? userId = null, bool isActive = true)
        {
            return new DoctorProfile
            {
                Id = id ?? DefaultDoctorProfileId,
                UserId = userId ?? DefaultDoctorUserId,
                IsActive = isActive
            };
        }

        public static User GetUser(Guid? id = null, string? avatarUrl = "https://example.com/avatar.jpg")
        {
            return new User
            {
                Id = id ?? Guid.NewGuid(),
                AvatarUrl = avatarUrl
            };
        }

        public static PatientProfile GetPatientProfile(
            Guid? id = null,
            string fullName = "Nguyen Van A",
            string? phone = "0987654321",
            User? user = null)
        {
            var userEntity = user ?? GetUser();
            return new PatientProfile
            {
                Id = id ?? Guid.NewGuid(),
                UserId = userEntity.Id,
                User = userEntity,
                FullName = fullName,
                PhoneNumber = phone,
                Gender = Gender.MALE,
                Dob = new DateTime(1990, 1, 1)
            };
        }

        public static Appointment GetAppointment(
            Guid? id = null,
            Guid? doctorId = null,
            PatientProfile? patient = null,
            DateTime? appointmentDate = null,
            AppointmentStatus status = AppointmentStatus.PENDING)
        {
            var patientProfile = patient ?? GetPatientProfile();
            return new Appointment
            {
                Id = id ?? Guid.NewGuid(),
                DoctorId = doctorId ?? DefaultDoctorProfileId,
                PatientId = patientProfile.Id,
                Patient = patientProfile,
                AppointmentDate = appointmentDate ?? DateTime.Today,
                Status = status
            };
        }
    }
}
