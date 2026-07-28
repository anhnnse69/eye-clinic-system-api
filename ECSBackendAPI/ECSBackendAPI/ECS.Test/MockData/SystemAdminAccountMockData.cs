using ECS.Application.Services.SystemAdminServices.AdminSystemCreateAccountServices;
using ECS.Application.Services.SystemAdminServices.AdminSystemDeleteAccountServices;
using ECS.Application.Services.SystemAdminServices.AdminSystemEditAccountServices;
using ECS.Application.Services.SystemAdminServices.AdminSystemListAccountServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Reusable mock data for System Admin account CRUD unit tests.
    /// </summary>
    public static class SystemAdminAccountMockData
    {
        public static Guid SystemAdminUserId =>
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        public static Guid TargetAccountUserId =>
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        public static Guid OtherAccountUserId =>
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        public static User GetSystemAdminUser() => new User
        {
            Id = SystemAdminUserId,
            FullName = "System Admin",
            Email = "sysadmin@ECS.vn",
            Phone = "0900000001",
            PasswordHash = "x",
            Role = UserRole.SYSTEM_ADMIN,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddYears(-2),
            UpdatedAt = DateTime.UtcNow.AddYears(-1)
        };

        public static User GetTargetAccount() => new User
        {
            Id = TargetAccountUserId,
            FullName = "Nguyen Van Target",
            Email = "target@ECS.vn",
            Phone = "0901234567",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@12345"),
            Role = UserRole.PATIENT,
            IsActive = true,
            AvatarUrl = "https://example.com/avatar.png",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
        };

        public static User GetInactiveTargetAccount()
        {
            var user = GetTargetAccount();
            user.IsActive = false;
            return user;
        }

        public static User GetUserWithPhone(string phone) => new User
        {
            Id = OtherAccountUserId,
            FullName = "Existing Phone Owner",
            Email = "phone-owner@ECS.vn",
            Phone = phone,
            PasswordHash = "x",
            Role = UserRole.PATIENT,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };

        public static User GetUserWithEmail(string email) => new User
        {
            Id = OtherAccountUserId,
            FullName = "Existing Email Owner",
            Email = email,
            Phone = "0999888777",
            PasswordHash = "x",
            Role = UserRole.PATIENT,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };

        public static CreateAccountRequest GetValidCreateAccountRequest() => new CreateAccountRequest
        {
            Phone = "0911222333",
            Email = "new-account@ECS.vn",
            Password = "Secure@12345",
            FullName = "New Account User",
            Role = UserRole.RECEPTIONIST,
            AvatarUrl = "https://example.com/new-avatar.png"
        };

        public static EditAccountRequest GetValidEditAccountRequest() => new EditAccountRequest
        {
            Id = TargetAccountUserId,
            Phone = "0911333444",
            Email = "updated@ECS.vn",
            FullName = "Updated Account User",
            Role = UserRole.DOCTOR,
            AvatarUrl = "https://example.com/updated-avatar.png"
        };

        public static DeleteAccountRequest GetDeactivateAccountRequest() => new DeleteAccountRequest
        {
            UserId = TargetAccountUserId,
            IsActive = false
        };

        public static DeleteAccountRequest GetActivateAccountRequest() => new DeleteAccountRequest
        {
            UserId = TargetAccountUserId,
            IsActive = true
        };

        public static GetAccountsRequest GetValidGetAccountsRequest() => new GetAccountsRequest
        {
            PageNumber = 1,
            PageSize = 10
        };
    }
}
