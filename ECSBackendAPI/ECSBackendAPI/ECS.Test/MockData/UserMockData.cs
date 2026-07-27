using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Provides reusable mock User data for unit tests.
    /// </summary>
    public static class UserMockData
    {
        /// <summary>
        /// Returns a valid, active user with hashed password.
        /// Password plain text: "Test@12345"
        /// </summary>
        public static User GetValidActiveUser() => new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            FullName = "Nguyen Van A",
            Email = "nguyenvana@ECS.vn",
            Phone = "0901234567",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@12345"),
            Role = UserRole.PATIENT,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
        };

        /// <summary>
        /// Returns a user whose IsActive = false (deactivated account).
        /// </summary>
        public static User GetInactiveUser() => new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            FullName = "Tran Thi B",
            Email = "tranthib@ECS.vn",
            Phone = "0912345678",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@12345"),
            Role = UserRole.PATIENT,
            IsActive = false,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };

        /// <summary>
        /// Returns a doctor user.
        /// </summary>
        public static User GetDoctorUser() => new User
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            FullName = "BS. Le Van C",
            Email = "levanc@ECS.vn",
            Phone = "0923456789",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Doctor@12345"),
            Role = UserRole.DOCTOR,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-30)
        };

        /// <summary>
        /// Returns a receptionist user.
        /// </summary>
        public static User GetReceptionistUser() => new User
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            FullName = "Le Thi D",
            Email = "lethid@ECS.vn",
            Phone = "0934567890",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Reception@12345"),
            Role = UserRole.RECEPTIONIST,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-15),
            UpdatedAt = DateTime.UtcNow.AddDays(-15)
        };

        // ── Aggregate / Lookup helpers used by personal-profile tests ───────────

        /// <summary>
        /// Deterministic clinic instance used to wire DoctorProfile/StaffClinic navigation.
        /// </summary>
        public static Clinic GetTestClinic() => new Clinic
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "Bệnh viện Mắt Sài Gòn",
            Address = "123 Nguyen Hue, District 1, HCMC",
            Phone = "02873001234",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddYears(-2),
            UpdatedAt = DateTime.UtcNow.AddYears(-1)
        };

        /// <summary>
        /// Deterministic specialty instance.
        /// </summary>
        public static Specialty GetTestSpecialty() => new Specialty
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Name = "Khoa Mắt Nhi",
            Description = "Pediatric Ophthalmology",
            IsActive = true
        };

        /// <summary>
        /// Returns an active DoctorProfile bound to the given user/clinic/specialty.
        /// </summary>
        public static DoctorProfile GetActiveDoctorProfile(User user, Clinic clinic, Specialty? specialty = null) => new DoctorProfile
        {
            Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            UserId = user.Id,
            ClinicId = clinic.Id,
            Clinic = clinic,
            SpecialtyId = specialty?.Id,
            Specialty = specialty,
            Title = "Senior Ophthalmologist",
            ExperienceYears = 12,
            Bio = "Experienced eye-care professional.",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddYears(-2),
            UpdatedAt = DateTime.UtcNow.AddYears(-1)
        };

        /// <summary>
        /// Returns an inactive DoctorProfile bound to the given user/clinic.
        /// </summary>
        public static DoctorProfile GetInactiveDoctorProfile(User user, Clinic clinic) => new DoctorProfile
        {
            Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            UserId = user.Id,
            ClinicId = clinic.Id,
            Clinic = clinic,
            SpecialtyId = null,
            Specialty = null,
            Title = "Old Title",
            ExperienceYears = 3,
            Bio = "Old Bio",
            IsActive = false,
            CreatedAt = DateTime.UtcNow.AddYears(-5),
            UpdatedAt = DateTime.UtcNow.AddYears(-4)
        };

        /// <summary>
        /// Returns an active StaffClinic bound to the given user/clinic.
        /// </summary>
        public static StaffClinic GetActiveStaffClinic(User user, Clinic clinic) => new StaffClinic
        {
            Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            UserId = user.Id,
            ClinicId = clinic.Id,
            Clinic = clinic,
            Role = StaffRole.RECEPTIONIST,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddYears(-1),
            UpdatedAt = DateTime.UtcNow.AddMonths(-6)
        };

        /// <summary>
        /// Returns an inactive StaffClinic bound to the given user/clinic.
        /// </summary>
        public static StaffClinic GetInactiveStaffClinic(User user, Clinic clinic) => new StaffClinic
        {
            Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            UserId = user.Id,
            ClinicId = clinic.Id,
            Clinic = clinic,
            Role = StaffRole.RECEPTIONIST,
            IsActive = false,
            CreatedAt = DateTime.UtcNow.AddYears(-3),
            UpdatedAt = DateTime.UtcNow.AddYears(-2)
        };
    }
}
