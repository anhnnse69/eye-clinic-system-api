using ECS.Application.Common.OTP;
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
        /// Step 1: Validate input request
        /// Step 2: Retrieve user by email
        /// Step 3: Validate user existence
        /// Step 4: Generate reset token and send OTP
        /// Step 5: Create response
        /// </summary>
        public async Task<ApiResponse<ForgotPasswordResponse>> Process(ForgotPasswordRequest request)
        {
            // Step 1: Validate input request
            bool isValidationPassed = true;
            string? validationErrorCode = null;
            ValidateRequest(request, ref isValidationPassed, ref validationErrorCode);
            // Step 2: Retrieve user by email
            var retrievedUser = await RetrieveUserByEmail(request.Email.ToLower().Trim());
            // Step 3: Validate user existence
            bool isUserFound = IsUserFound(retrievedUser);
            // Step 4: Generate reset token and send OTP
            var resetToken = await GenerateAndSendOtp(retrievedUser, isValidationPassed, isUserFound);
            // Step 5: Create response
            return CreateResponse(validationErrorCode, isValidationPassed, isUserFound, resetToken);
        }

        /// <summary>
        /// Step 1: Validate input request using FluentValidation.
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
        /// Step 2: Retrieve user by normalized email address from database.
        /// </summary>
        private async Task<User?> RetrieveUserByEmail(string email)
        {
            return await _userQueryRepository
                .FindByCondition(x =>
                    x.Email != null &&
                    x.Email.ToLower() == email &&
                    x.IsActive)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Step 3: Check if user exists in database.
        /// </summary>
        private bool IsUserFound(User? user)
        {
            return user != null;
        }

        /// <summary>
        /// Step 4: Generate reset token and send OTP to user's email.
        /// </summary>
        private async Task<string?> GenerateAndSendOtp(User? user, bool isValidationPassed, bool isUserFound)
        {
            if (!CanGenerateOtp(isValidationPassed, isUserFound, user))
            {
                return null;
            }

            var resetToken = GenerateResetToken();
            var otp = GenerateOtp();
            var emailBody = _emailService.BuildPasswordResetEmailBody(otp);
            var subject = "ECS Medical - Password Reset OTP";
            var isSent = await _emailService.SendEmailAsync(user!.Email!, subject, emailBody);

            if (isSent)
            {
                OtpCacheManager.Store(resetToken, user.Email!, otp);
            }

            return isSent ? resetToken : null;
        }

        /// <summary>
        /// Check if OTP can be generated based on validation status and user data.
        /// </summary>
        private bool CanGenerateOtp(bool isValidationPassed, bool isUserFound, User? user)
        {
            return isValidationPassed && isUserFound && user != null;
        }

        /// <summary>
        /// Generate a unique reset token.
        /// </summary>
        private static string GenerateResetToken()
        {
            return Guid.NewGuid().ToString("N") + DateTime.UtcNow.Ticks.ToString("X");
        }

        /// <summary>
        /// Generate a 6-digit OTP code.
        /// </summary>
        private static string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        /// <summary>
        /// Step 5: Create API response based on validation flags.
        /// </summary>
        private ApiResponse<ForgotPasswordResponse> CreateResponse(
            string? validationErrorCode,
            bool isValidationPassed,
            bool isUserFound,
            string? resetToken)
        {
            var errorResponse = CreateErrorResponse(
                validationErrorCode,
                isValidationPassed,
                isUserFound,
                resetToken);

            return errorResponse ?? CreateSuccessResponse(resetToken!);
        }

        /// <summary>
        /// Create error response based on validation flags.
        /// </summary>
        private ApiResponse<ForgotPasswordResponse>? CreateErrorResponse(
            string? validationErrorCode,
            bool isValidationPassed,
            bool isUserFound,
            string? resetToken)
        {
            if (!isValidationPassed)
            {
                return ApiResponse<ForgotPasswordResponse>.FailWithNull(validationErrorCode!);
            }
            if (!isUserFound)
            {
                return ApiResponse<ForgotPasswordResponse>.FailWithNull(GeneralCode.APP_MESSAGE_4020.ToString());
            }
            if (resetToken == null)
            {
                return ApiResponse<ForgotPasswordResponse>.FailWithNull(GeneralCode.APP_MESSAGE_5003.ToString());
            }
            return null;
        }

        /// <summary>
        /// Create success response with reset token.
        /// </summary>
        private ApiResponse<ForgotPasswordResponse> CreateSuccessResponse(string resetToken)
        {
            var response = new ForgotPasswordResponse { ResetToken = resetToken };
            return ApiResponse<ForgotPasswordResponse>.Success(GeneralCode.APP_MESSAGE_2000.ToString(), response);
        }
    }
}
