using ECS.Application.Common.Response;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.ConfigService.JwtService;
using ECS.Infrastructure.Helper.Utility;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.AuthServices.LoginServices
{
    /// <summary>
    /// Handles the login business logic for authenticated users.
    /// </summary>
    public class LoginService : ILoginService
    {
        private readonly IRepositoryQueryBase<User, Guid, AppDbContext> _userRepository;
        private readonly IJwtTokenService _jwtTokenService;

        /// <summary>
        /// Initializes a new instance of <see cref="LoginService"/> with required dependencies.
        /// </summary>
        /// <param name="userRepository">Repository for querying user data.</param>
        /// <param name="jwtTokenService">Service for generating JWT tokens.</param>
        public LoginService(
            IRepositoryQueryBase<User, Guid, AppDbContext> userRepository,
            IJwtTokenService jwtTokenService)
        {
            _userRepository = userRepository;
            _jwtTokenService = jwtTokenService;
        }

        /// <summary>
        /// Processes the login request by validating credentials and returning a JWT token on success.
        /// </summary>
        /// <param name="loginRequest">The login request containing email and password.</param>
        /// <returns>An <see cref="ApiResponse{LoginResponse}"/> containing the token or an error code.</returns>
        public async Task<ApiResponse<LoginResponse>> Proccess(LoginRequest loginRequest)
        {
            // Initialize validation flags
            bool isRetrivedDataValid = true;
            bool isPasswordCorrect = true;
            // Retrieve user by normalized email
            var retirvedUser = await RetrieveUserData(loginRequest.EmailAddress.ToLower());
            // Validate retrieved user and password
            ValidateRetrivedData(retirvedUser, ref isRetrivedDataValid, ref isPasswordCorrect, loginRequest);
            // Build and return the appropriate response
            return await CreateResponse(retirvedUser, isRetrivedDataValid, isPasswordCorrect);
        }

        /// <summary>
        /// Queries the repository for an active user matching the given email address.
        /// </summary>
        /// <param name="emailAddress">The normalized (lowercased) email address to search for.</param>
        /// <returns>The matching <see cref="User"/> if found and active; otherwise <c>null</c>.</returns>
        private async Task<User?> RetrieveUserData(string emailAddress)
        {
            // Filter by email (case-insensitive) and active status
            return await _userRepository
                .FindByCondition(x => x.Email != null &&
                                      x.Email.ToLower() == emailAddress &&
                                      x.IsActive)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Generates a JWT token for the given authenticated user.
        /// </summary>
        /// <param name="user">The authenticated user entity.</param>
        /// <returns>A signed JWT token string.</returns>
        private string CreateToken(User user)
        {
            // Delegate token generation to the JWT service
            return _jwtTokenService.GenerateToken(user);
        }

        /// <summary>
        /// Builds the API response based on the validation results.
        /// Returns a failure response if credentials are invalid, or a success response with the JWT token.
        /// </summary>
        /// <param name="retirvedUser">The user retrieved from the database.</param>
        /// <param name="isRetrieveDataValid">Indicates whether a user record was found.</param>
        /// <param name="isPasswordCorrect">Indicates whether the provided password matches the stored hash.</param>
        /// <returns>An <see cref="ApiResponse{LoginResponse}"/> with a token on success or an error code on failure.</returns>
        private async Task<ApiResponse<LoginResponse>> CreateResponse(
            User? retirvedUser, bool isRetrieveDataValid, bool isPasswordCorrect)
        {
            // Return 4016 if user not found or password mismatch
            if (!isPasswordCorrect || !isRetrieveDataValid)
                return ApiResponse<LoginResponse>.Fail(GeneralCode.APP_MESSAGE_4016.ToString());
            // Generate token and return success response
            var token = CreateToken(retirvedUser!);
            return ApiResponse<LoginResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                new LoginResponse(token)
            );
        }

        /// <summary>
        /// Validates the retrieved user data and verifies the provided password against the stored hash.
        /// Sets the corresponding flags to <c>false</c> if validation fails.
        /// </summary>
        /// <param name="retirvedData">The user entity retrieved from the database, or <c>null</c> if not found.</param>
        /// <param name="isRetrievedData">Flag indicating whether the user record exists.</param>
        /// <param name="isPasswordCorrect">Flag indicating whether the password is correct.</param>
        /// <param name="loginRequest">The original login request containing the raw password.</param>
        private void ValidateRetrivedData(
            User? retirvedData,
            ref bool isRetrievedData,
            ref bool isPasswordCorrect,
            LoginRequest loginRequest)
        {
            // Mark as invalid if no user record was found
            if (retirvedData == null)
            {
                isRetrievedData = false;
                return;
            }
            // Verify the provided password against the stored hash
            if (!PasswordHelper.VerifyPassword(loginRequest.Password, retirvedData.PasswordHash))
                isPasswordCorrect = false;
        }
    }
}