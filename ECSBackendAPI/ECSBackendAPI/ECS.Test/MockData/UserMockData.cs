using ECS.Domain.Entities.Auth;
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
    }
}