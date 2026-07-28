using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;

namespace ECS.Test.MockData
{
    public static class GetActiveDoctorsMockData
    {
        public static readonly Guid ReceptionistUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid DoctorUserId1 = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid DoctorProfileId1 = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid DoctorUserId2 = Guid.Parse("55555555-5555-5555-5555-555555555555");
        public static readonly Guid DoctorProfileId2 = Guid.Parse("66666666-6666-6666-6666-666666666666");

        public static User GetReceptionistUser(Guid? userId = null) => new()
        {
            Id = userId ?? ReceptionistUserId,
            FullName = "Le Thi Receptionist",
            Email = "receptionist@clinic.vn",
            Phone = "0901234567",
            PasswordHash = "x",
            Role = Domain.Enums.UserRole.RECEPTIONIST,
            IsActive = true
        };

        public static Clinic GetClinic(Guid? id = null) => new()
        {
            Id = id ?? ClinicId,
            Name = "Test Clinic",
            Address = "123 Test Street",
            Phone = "02812345678",
            Email = "test@clinic.vn",
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(20, 0),
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddYears(-1),
            UpdatedAt = DateTime.UtcNow.AddMonths(-6)
        };

        public static Specialty GetSpecialty(string name = "Khoa Mắt") => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Eye specialty",
            IsActive = true
        };

        public static User GetDoctorUser(Guid? id = null, string fullName = "BS. Le Van C") => new()
        {
            Id = id ?? DoctorUserId1,
            FullName = fullName,
            Email = "doctor@clinic.vn",
            Phone = "0912345678",
            PasswordHash = "x",
            Role = Domain.Enums.UserRole.DOCTOR,
            IsActive = true,
            AvatarUrl = "https://example.com/avatar.jpg"
        };

        public static DoctorProfile GetActiveDoctorProfile(
            Guid? id = null,
            Guid? userId = null,
            Guid? clinicId = null,
            User? user = null,
            Specialty? specialty = null,
            string fullName = "BS. Le Van C")
        {
            var doctorUser = user ?? GetDoctorUser(userId ?? DoctorUserId1, fullName);
            return new DoctorProfile
            {
                Id = id ?? DoctorProfileId1,
                UserId = doctorUser.Id,
                ClinicId = clinicId ?? ClinicId,
                Clinic = GetClinic(clinicId ?? ClinicId),
                SpecialtyId = specialty?.Id,
                Specialty = specialty,
                Title = "Senior Doctor",
                ExperienceYears = 10,
                Bio = "Experienced doctor",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-2),
                UpdatedAt = DateTime.UtcNow.AddYears(-1),
                User = doctorUser
            };
        }

        public static DoctorProfile GetInactiveDoctorProfile(
            Guid? id = null,
            Guid? userId = null,
            Guid? clinicId = null) => new()
        {
            Id = id ?? DoctorProfileId1,
            UserId = userId ?? DoctorUserId1,
            ClinicId = clinicId ?? ClinicId,
            Clinic = GetClinic(clinicId ?? ClinicId),
            SpecialtyId = null,
            Specialty = null,
            Title = "Old Doctor",
            ExperienceYears = 3,
            Bio = "Old bio",
            IsActive = false,
            CreatedAt = DateTime.UtcNow.AddYears(-5),
            UpdatedAt = DateTime.UtcNow.AddYears(-4),
            User = GetDoctorUser(DoctorUserId1)
        };

        public static StaffClinic GetActiveStaffClinic(
            Guid? userId = null,
            Guid? clinicId = null,
            Domain.Enums.StaffRole role = Domain.Enums.StaffRole.RECEPTIONIST) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? ReceptionistUserId,
            ClinicId = clinicId ?? ClinicId,
            Clinic = GetClinic(clinicId ?? ClinicId),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddYears(-1),
            UpdatedAt = DateTime.UtcNow.AddMonths(-6)
        };

        public static StaffClinic GetInactiveStaffClinic(
            Guid? userId = null,
            Guid? clinicId = null) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? ReceptionistUserId,
            ClinicId = clinicId ?? ClinicId,
            Clinic = GetClinic(clinicId ?? ClinicId),
            Role = Domain.Enums.StaffRole.RECEPTIONIST,
            IsActive = false,
            CreatedAt = DateTime.UtcNow.AddYears(-2),
            UpdatedAt = DateTime.UtcNow.AddYears(-1)
        };
    }
}
