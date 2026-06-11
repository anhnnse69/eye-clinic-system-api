using ECS.Application.Common.Response;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.ConfigService.EmailService;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.AuthServices.ForgotPasswordServices
{
    /// <summary>
    /// Handles forgot password business logic.
    /// </summary>
    public class ForgotPasswordService : IForgotPasswordService
    {
        private readonly IRepositoryQueryBase<User, Guid, AppDbContext> _userQueryRepository;
        private readonly IRepositoryBaseAsync<User, Guid, AppDbContext> _userRepository;
        private readonly IValidator<ForgotPasswordRequest> _validator;
        private readonly IEmailService _emailService;

        private static readonly Dictionary<string, OtpData> _otpCache = new();
        private static readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance of <see cref="ForgotPasswordService"/>.
        /// </summary>
        public ForgotPasswordService(
            IRepositoryQueryBase<User, Guid, AppDbContext> userQueryRepository,
            IRepositoryBaseAsync<User, Guid, AppDbContext> userRepository,
            IValidator<ForgotPasswordRequest> validator,
            IEmailService emailService)
        {
            _userQueryRepository = userQueryRepository;
            _userRepository = userRepository;
            _validator = validator;
            _emailService = emailService;
        }

        /// <summary>
        /// Processes the forgot password request by sending OTP to user's email.
        /// </summary>
        /// <param name="request">Forgot password request containing email.</param>
        /// <returns>An <see cref="ApiResponse{Object}"/> indicating success or an error code.</returns>
        public async Task<ApiResponse<object>> Process(ForgotPasswordRequest request)
        {
            // Initialize status tracking flags
            bool isValidationPassed = true;
            bool isUserFound = true;
            string? validationErrorCode = null;
            // Step 1: Validate request data format
            ValidateRequest(request, ref isValidationPassed, ref validationErrorCode);
            // Step 2: Retrieve user by normalized email
            var retrievedUser = await RetrieveUserByEmail(request.Email.ToLower().Trim(), isValidationPassed);
            // Step 3: Verify user existence
            ValidateUserExistence(retrievedUser, isValidationPassed, ref isUserFound);
            // Step 4: Send OTP to user's email
            var isEmailSent = await SendOtpToEmail(retrievedUser, isValidationPassed, isUserFound);
            // Step 5: Assemble API payload or generate error response
            return CreateResponse(validationErrorCode, isValidationPassed, isUserFound, isEmailSent);
        }

        /// <summary>
        /// Validates the incoming request payload.
        /// </summary>
        private void ValidateRequest(
            ForgotPasswordRequest request,
            ref bool isValidationPassed,
            ref string? validationErrorCode)
        {
            var result = _validator.Validate(request);
            isValidationPassed = result.IsValid;
            validationErrorCode = result.IsValid ? null : result.Errors.First().ErrorCode;
        }

        /// <summary>
        /// Retrieves user by normalized email address.
        /// </summary>
        private async Task<User?> RetrieveUserByEmail(string email, bool isValidationPassed)
        {
            if (!isValidationPassed)
            {
                return null;
            }
            return await _userQueryRepository
                .FindByCondition(x =>
                    x.Email != null &&
                    x.Email.ToLower() == email &&
                    x.IsActive)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Validates user existence.
        /// </summary>
        private void ValidateUserExistence(
            User? user,
            bool isValidationPassed,
            ref bool isUserFound)
        {
            isUserFound = true;
            if (!isValidationPassed)
            {
                isUserFound = false;
                return;
            }
            isUserFound = user != null;
        }

        /// <summary>
        /// Sends OTP to user's email.
        /// </summary>
        private async Task<bool> SendOtpToEmail(User? user, bool isValidationPassed, bool isUserFound)
        {
            if (!isValidationPassed || !isUserFound || user == null)
            {
                return true;
            }
            var otp = GenerateOtp();
            var emailBody = _emailService.BuildPasswordResetEmailBody(otp);
            var subject = "ECS Medical - Password Reset OTP";
            var isSent = await _emailService.SendEmailAsync(user.Email!, subject, emailBody);
            if (isSent)
            {
                StoreOtp(user.Email!, otp);
            }
            return isSent;
        }

        /// <summary>
        /// Generates a 6-digit OTP code.
        /// </summary>
        private static string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        /// <summary>
        /// Stores OTP in memory cache with 5-minute expiration.
        /// </summary>
        private static void StoreOtp(string email, string otp)
        {
            lock (_lock)
            {
                _otpCache[email.ToLower()] = new OtpData(otp, DateTime.UtcNow.AddMinutes(5));
            }
        }

        /// <summary>
        /// Creates the API response based on validation flags.
        /// </summary>
        private ApiResponse<object> CreateResponse(
            string? validationErrorCode,
            bool isValidationPassed,
            bool isUserFound,
            bool isEmailSent)
        {
            var errorResponse = CreateErrorResponse(
                validationErrorCode, isValidationPassed, isUserFound, isEmailSent);
            if (errorResponse != null)
            {
                return errorResponse;
            }
            return CreateSuccessResponse();
        }

        /// <summary>
        /// Creates an error response based on validation flags.
        /// </summary>
        private ApiResponse<object>? CreateErrorResponse(
            string? validationErrorCode,
            bool isValidationPassed,
            bool isUserFound,
            bool isEmailSent)
        {
            if (!isValidationPassed)
            {
                return ApiResponse<object>.FailWithNull(validationErrorCode!);
            }
            if (!isUserFound)
            {
                return ApiResponse<object>.FailWithNull(
                    GeneralCode.APP_MESSAGE_4020.ToString());
            }
            if (!isEmailSent)
            {
                return ApiResponse<object>.FailWithNull(
                    GeneralCode.APP_MESSAGE_5003.ToString());
            }
            return null;
        }

        /// <summary>
        /// Creates a success response.
        /// </summary>
        private ApiResponse<object> CreateSuccessResponse()
        {
            return ApiResponse<object>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                true);
        }

        private record OtpData(string Otp, DateTime ExpiresAt);
    }
}
