using ECS.Application.Common.Response;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.ConfigService.JwtService;
using ECS.Infrastructure.Helper.Utility;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
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
        private readonly IValidator<LoginRequest> _validator;

        /// <summary>
        /// Initializes a new instance of <see cref="LoginService"/> with required dependencies.
        /// </summary>
        /// <param name="userRepository">Repository for querying user data.</param>
        /// <param name="jwtTokenService">Service for generating JWT tokens.</param>
        /// <param name="validator">Validator for login request data.</param>
        public LoginService(
            IRepositoryQueryBase<User, Guid, AppDbContext> userRepository,
            IJwtTokenService jwtTokenService,
            IValidator<LoginRequest> validator)
        {
            _userRepository = userRepository;
            _jwtTokenService = jwtTokenService;
            _validator = validator;
        }

        /// <summary>
        /// Processes the login request by validating credentials and returning a JWT token on success.
        /// </summary>
        /// <param name="loginRequest">The login request containing email and password.</param>
        /// <returns>An <see cref="ApiResponse{LoginResponse}"/> containing the token or an error code.</returns>
        public async Task<ApiResponse<LoginResponse>> Proccess(LoginRequest loginRequest)
        {
            // Initialize status tracking flags
            bool isValidationPassed = true;
            bool isPasswordCorrect = true;
            string? validationErrorCode = null;
            // Step 1: Validate request data format
            ValidateRequest(loginRequest, ref isValidationPassed, ref validationErrorCode);
            // Step 2: Retrieve user by normalized email
            var retrievedUser = await RetrieveUserData(loginRequest.EmailAddress.ToLower());
            // Step 3: Verify user existence and password match
            ValidateCredentials(retrievedUser, loginRequest, ref isPasswordCorrect, isValidationPassed);
            // Step 4: Assemble API payload or generate error response
            return CreateResponse(retrievedUser, isValidationPassed, isPasswordCorrect, validationErrorCode);
        }

        /// <summary>
        /// Validates the incoming request payload against defined business rules.
        /// </summary>
        /// <param name="loginRequest">The request payload to validate.</param>
        /// <param name="isValidationPassed">Flag updated to <c>false</c> if validation fails.</param>
        /// <param name="validationErrorCode">Stores the first validation error code encountered.</param>
        private void ValidateRequest(
            LoginRequest loginRequest,
            ref bool isValidationPassed,
            ref string? validationErrorCode)
        {
            var result = _validator.Validate(loginRequest);
            if (!result.IsValid)
            {
                isValidationPassed = false;
                validationErrorCode = result.Errors.First().ErrorCode;
            }
        }

        /// <summary>
        /// Validates the retrieved user data and verifies the provided password against the stored hash.
        /// </summary>
        /// <param name="retrievedUser">The user entity retrieved from the database, or <c>null</c> if not found.</param>
        /// <param name="loginRequest">The original login request containing the raw password.</param>
        /// <param name="isPasswordCorrect">Flag updated to <c>false</c> if password verification fails.</param>
        /// <param name="isValidationPassed">Precondition flag indicating if request validation succeeded.</param>
        private void ValidateCredentials(
            User? retrievedUser,
            LoginRequest loginRequest,
            ref bool isPasswordCorrect,
            bool isValidationPassed)
        {
            if (!isValidationPassed)
            {
                return;
            }
            if (retrievedUser == null)
            {
                isPasswordCorrect = false;
                return;
            }
            var isVerified = PasswordHelper.VerifyPassword(
                loginRequest.Password, retrievedUser.PasswordHash);
            if (!isVerified)
            {
                isPasswordCorrect = false;
            }
        }

        /// <summary>
        /// Queries the repository for an active user matching the given email address.
        /// </summary>
        /// <param name="emailAddress">The normalized (lowercased) email address to search for.</param>
        /// <returns>The matching <see cref="User"/> if found and active; otherwise <c>null</c>.</returns>
        private async Task<User?> RetrieveUserData(string emailAddress)
        {
            return await _userRepository
                .FindByCondition(x =>
                    x.Email != null &&
                    x.Email.ToLower() == emailAddress &&
                    x.IsActive)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Generates an API response payload based on validation status flags.
        /// </summary>
        /// <param name="retrievedUser">The resolved user entity instance.</param>
        /// <param name="isValidationPassed">Indicates whether request format validation succeeded.</param>
        /// <param name="isPasswordCorrect">Indicates whether user credentials matched.</param>
        /// <param name="validationErrorCode">The error code from validation failure, if any.</param>
        /// <returns>A configured <see cref="ApiResponse{LoginResponse}"/>.</returns>
        private ApiResponse<LoginResponse> CreateResponse(
            User? retrievedUser,
            bool isValidationPassed,
            bool isPasswordCorrect,
            string? validationErrorCode)
        {
            var errorResponse = CreateErrorResponse(
                isValidationPassed, isPasswordCorrect, validationErrorCode);
            if (errorResponse != null)
            {
                return errorResponse;
            }
            return CreateSuccessResponse(retrievedUser!);
        }

        /// <summary>
        /// Creates an error response based on validation failure flags.
        /// </summary>
        /// <param name="isValidationPassed">Indicates whether request validation succeeded.</param>
        /// <param name="isPasswordCorrect">Indicates whether credentials matched.</param>
        /// <param name="validationErrorCode">The error code from validation failure.</param>
        /// <returns>A failed <see cref="ApiResponse{LoginResponse}"/> variant if errors are found; otherwise <c>null</c>.</returns>
        private ApiResponse<LoginResponse>? CreateErrorResponse(
            bool isValidationPassed,
            bool isPasswordCorrect,
            string? validationErrorCode)
        {
            if (!isValidationPassed)
            {
                return ApiResponse<LoginResponse>.Fail(validationErrorCode!);
            }
            if (!isPasswordCorrect)
            {
                return ApiResponse<LoginResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4016.ToString());
            }
            return null;
        }

        /// <summary>
        /// Creates a success response containing the JWT token.
        /// </summary>
        /// <param name="user">The authenticated user entity.</param>
        /// <returns>A success <see cref="ApiResponse{LoginResponse}"/> with the token payload.</returns>
        private ApiResponse<LoginResponse> CreateSuccessResponse(User user)
        {
            var token = _jwtTokenService.GenerateToken(user);
            return ApiResponse<LoginResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                new LoginResponse(token));
        }
    }
}
