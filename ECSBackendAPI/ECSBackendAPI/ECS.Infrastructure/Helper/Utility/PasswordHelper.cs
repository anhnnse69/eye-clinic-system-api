namespace ECS.Infrastructure.Helper.Utility
{
    /// <summary>
    /// Provides helper methods for securely hashing and verifying passwords using the BCrypt algorithm.
    /// </summary>
    /// <remarks>This class is intended for use in scenarios where secure password storage and verification
    /// are required, such as user authentication systems. All methods are static and thread-safe.</remarks>
    public static class PasswordHelper
    {
        /// <summary>
        /// Generates a secure hash for the specified password using the BCrypt algorithm.
        /// </summary>
        /// <remarks>The generated hash includes a salt and is suitable for secure password storage. The
        /// hash can be verified using BCrypt-compatible verification methods.</remarks>
        /// <param name="password">The plain text password to hash. Cannot be null or empty.</param>
        /// <returns>A string containing the hashed representation of the password.</returns>
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        /// <summary>
        /// Verifies that the specified plain-text password matches the provided BCrypt hashed password.
        /// </summary>
        /// <param name="password">The plain-text password to verify.</param>
        /// <param name="hashedPassword">The BCrypt hashed password to compare against. Must be a valid BCrypt hash string.</param>
        /// <returns>true if the password matches the hashed password; otherwise, false.</returns>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
