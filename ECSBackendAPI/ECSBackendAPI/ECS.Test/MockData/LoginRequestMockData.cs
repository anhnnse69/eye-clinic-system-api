using ECS.Application.Services.AuthServices.LoginServices;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Provides reusable mock LoginRequest data for unit tests.
    /// </summary>
    public static class LoginRequestMockData
    {
        /// <summary>
        /// Valid credentials matching <see cref="UserMockData.GetValidActiveUser"/>.
        /// </summary>
        public static LoginRequest GetValidRequest() => new LoginRequest
        {
            EmailAddress = "nguyenvana@ECS.vn",
            Password = "Test@12345"
        };

        /// <summary>
        /// Valid email but wrong password.
        /// </summary>
        public static LoginRequest GetWrongPasswordRequest() => new LoginRequest
        {
            EmailAddress = "nguyenvana@ECS.vn",
            Password = "WrongPass@99"
        };

        /// <summary>
        /// Email not registered in the system.
        /// </summary>
        public static LoginRequest GetNotFoundEmailRequest() => new LoginRequest
        {
            EmailAddress = "notfound@ECS.vn",
            Password = "Test@12345"
        };

        /// <summary>
        /// Email of deactivated user.
        /// </summary>
        public static LoginRequest GetInactiveUserRequest() => new LoginRequest
        {
            EmailAddress = "tranthib@ECS.vn",
            Password = "Test@12345"
        };

        /// <summary>
        /// Empty email — should fail validation.
        /// </summary>
        public static LoginRequest GetEmptyEmailRequest() => new LoginRequest
        {
            EmailAddress = string.Empty,
            Password = "Test@12345"
        };

        /// <summary>
        /// Invalid email format — should fail validation.
        /// </summary>
        public static LoginRequest GetInvalidEmailFormatRequest() => new LoginRequest
        {
            EmailAddress = "not-an-email",
            Password = "Test@12345"
        };

        /// <summary>
        /// Weak password (no uppercase, no special char) — should fail validation.
        /// </summary>
        public static LoginRequest GetWeakPasswordRequest() => new LoginRequest
        {
            EmailAddress = "nguyenvana@ECS.vn",
            Password = "weakpass"
        };
    }
}
