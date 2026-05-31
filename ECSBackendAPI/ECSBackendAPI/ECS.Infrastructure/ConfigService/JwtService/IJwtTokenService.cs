using ECS.Domain.Entities.Auth;

namespace ECS.Infrastructure.ConfigService.JwtService
{
    /// <summary>
    /// Defines a service for generating JSON Web Tokens (JWT) for authenticated users.
    /// </summary>
    public interface IJwtTokenService
    {
        /// <summary>
        /// Generates an authentication token for the specified user and returns its expiration time.
        /// </summary>
        /// <param name="user">The user for whom the authentication token is generated. Cannot be null.</param>
        /// <returns>A string representing the generated authentication token.</returns>
        string GenerateToken(User user);
    }
}
