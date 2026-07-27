using ECS.Application.Services.AuthServices.RegisterServices;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Provides reusable mock RegisterRequest data for unit tests.
    /// </summary>
    public static class RegisterRequestMockData
    {
        /// <summary>
        /// Valid registration request — passes all validator rules.
        /// </summary>
        public static RegisterRequest GetValidRequest() => new RegisterRequest
        {
            FullName = "Nguyen Van A",
            Email = "nguyenvana@ECS.vn",
            Phone = "0901234567",
            Password = "Test@12345",
            ConfirmPassword = "Test@12345"
        };

        /// <summary>
        /// Valid request whose email matches an existing user in DB (triggers 4017).
        /// </summary>
        public static RegisterRequest GetExistingEmailRequest() => new RegisterRequest
        {
            FullName = "Tran Thi B",
            Email = "existing@ECS.vn",
            Phone = "0912345678",
            Password = "Test@12345",
            ConfirmPassword = "Test@12345"
        };

        /// <summary>
        /// Valid request whose phone matches an existing user in DB (triggers 4018).
        /// </summary>
        public static RegisterRequest GetExistingPhoneRequest() => new RegisterRequest
        {
            FullName = "Le Van C",
            Email = "newuser@ECS.vn",
            Phone = "0909999999",
            Password = "Test@12345",
            ConfirmPassword = "Test@12345"
        };

        /// <summary>
        /// Invalid request — fails validation (e.g., empty FullName).
        /// </summary>
        public static RegisterRequest GetInvalidRequest() => new RegisterRequest
        {
            FullName = string.Empty,
            Email = "nguyenvana@ECS.vn",
            Phone = "0901234567",
            Password = "Test@12345",
            ConfirmPassword = "Test@12345"
        };

        /// <summary>
        /// Valid request with email that has uppercase + surrounding whitespace,
        /// used to verify normalization path (Trim + ToLower).
        /// </summary>
        public static RegisterRequest GetUpperCaseEmailRequest() => new RegisterRequest
        {
            FullName = "Nguyen Van A",
            Email = "  NGUYENVANA@ECS.VN  ",
            Phone = "0901234567",
            Password = "Test@12345",
            ConfirmPassword = "Test@12345"
        };
    }
}
