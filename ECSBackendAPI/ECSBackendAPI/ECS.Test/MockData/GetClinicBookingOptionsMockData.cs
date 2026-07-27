using ECS.Application.Services.PatientAppointmentManagementServices.GetClinicBookingOptionsServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class GetClinicBookingOptionsMockData
    {
        public static readonly Guid ValidClinicId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ValidDoctorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid ValidAnotherDoctorId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid ValidUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid ValidSpecialtyId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        public static Clinic GetClinic(Guid? id = null, bool isActive = true)
        {
            return new Clinic
            {
                Id = id ?? ValidClinicId,
                Name = "Phòng khám mắt",
                Address = "123 Lê Lợi",
                Phone = "0909090909",
                IsActive = isActive,
                IsPublished = true
            };
        }

        public static DoctorProfile GetDoctorProfile(
            Guid? id = null,
            Guid? clinicId = null,
            Guid? userId = null,
            string? fullName = null,
            bool isActive = true,
            Guid? specialtyId = null,
            string? specialtyName = null,
            int experienceYears = 5)
        {
            return new DoctorProfile
            {
                Id = id ?? ValidDoctorId,
                ClinicId = clinicId ?? ValidClinicId,
                UserId = userId ?? ValidUserId,
                IsActive = isActive,
                Title = "BS",
                ExperienceYears = experienceYears,
                User = new User
                {
                    Id = userId ?? ValidUserId,
                    FullName = fullName ?? "Dr. An",
                    Email = "doctor@example.com",
                    PasswordHash = "hash",
                    Phone = "0123456789"
                },
                Specialty = specialtyName != null
                    ? new Specialty
                    {
                        Id = specialtyId ?? ValidSpecialtyId,
                        Name = specialtyName,
                        IsActive = true
                    }
                    : null,
                Clinic = GetClinic(clinicId ?? ValidClinicId)
            };
        }
    }
}
