using ECS.Application.Common.OTP;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.Helper.Utility;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.AuthServices.ResetPasswordServices
{
    /// <summary>
    /// Handles reset password business logic.
    /// </summary>
    public class ResetPasswordService : IResetPasswordService
    {
        private readonly IRepositoryQueryBase<User, Guid, AppDbContext> _userQueryRepository;
        private readonly IRepositoryBaseAsync<User, Guid, AppDbContext> _userRepository;
        private readonly IValidator<ResetPasswordRequest> _validator;

        public ResetPasswordService(
            IRepositoryQueryBase<User, Guid, AppDbContext> userQueryRepository,
            IRepositoryBaseAsync<User, Guid, AppDbContext> userRepository,
            IValidator<ResetPasswordRequest> validator)
        {
            _userQueryRepository = userQueryRepository;
            _userRepository = userRepository;
            _validator = validator;
        }

        /// <summary>
        /// Processes reset password request with OTP verification.
        /// Step 1: Validate input request
        /// Step 2: Validate OTP and retrieve email from reset token
        /// Step 3: Retrieve user by email
        /// Step 4: Validate user existence
        /// Step 5: Update user password
        /// Step 6: Create response
        /// </summary>
        public async Task<ApiResponse<object>> Process(ResetPasswordRequest request)
        {
            // Step 1: Validate input request
            bool isValidationPassed = true;
            string? validationErrorCode = null;
            ValidateRequest(request, ref isValidationPassed, ref validationErrorCode);

            // Step 2: Validate OTP and retrieve email from reset token
            bool isOtpValid = true;
            string? otpErrorCode = null;
            string? emailFromToken = null;
            ValidateOtp(request.ResetToken, request.Otp, ref isOtpValid, ref otpErrorCode, ref emailFromToken);

            // Step 3: Retrieve user by email
            var retrievedUser = await RetrieveUserByEmail(emailFromToken!);

            // Step 4: Validate user existence
            bool isUserFound = IsUserFound(retrievedUser);

            // Step 5: Update user password
            bool isPasswordUpdated = await UpdateUserPassword(retrievedUser, request.NewPassword, isOtpValid, isUserFound);

            // Step 6: Create response
            return CreateResponse(
                validationErrorCode,
                isValidationPassed,
                isOtpValid,
                isUserFound,
                isPasswordUpdated,
                otpErrorCode);
        }

        /// <summary>
        /// Step 1: Validate input request using FluentValidation.
        /// </summary>
        private void ValidateRequest(
            ResetPasswordRequest request,
            ref bool isValidationPassed,
            ref string? validationErrorCode)
        {
            var result = _validator.Validate(request);
            isValidationPassed = result.IsValid;
            validationErrorCode = result.IsValid ? null : result.Errors.First().ErrorCode;
        }

        /// <summary>
        /// Step 2: Validate OTP using reset token and retrieve associated email.
        /// </summary>
        private void ValidateOtp(
            string resetToken,
            string otp,
            ref bool isValid,
            ref string? errorCode,
            ref string? email)
        {
            var (isOtpValid, returnedEmail, returnedErrorCode) = OtpCacheManager.Validate(resetToken, otp);
            isValid = isOtpValid;
            errorCode = returnedErrorCode;
            email = returnedEmail;
        }

        /// <summary>
        /// Step 3: Retrieve user by normalized email address from database.
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
        /// Step 4: Check if user exists in database.
        /// </summary>
        private bool IsUserFound(User? user)
        {
            return user != null;
        }

        /// <summary>
        /// Step 5: Update user password with new hashed password.
        /// </summary>
        private async Task<bool> UpdateUserPassword(User? user, string newPassword, bool isOtpValid, bool isUserFound)
        {
            if (!isOtpValid || user == null || !isUserFound)
            {
                return false;
            }
            try
            {
                var hashedPassword = PasswordHelper.HashPassword(newPassword);
                user.PasswordHash = hashedPassword;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Step 6: Create API response based on validation flags.
        /// </summary>
        private ApiResponse<object> CreateResponse(
            string? validationErrorCode,
            bool isValidationPassed,
            bool isOtpValid,
            bool isUserFound,
            bool isPasswordUpdated,
            string? otpErrorCode)
        {
            var errorResponse = CreateErrorResponse(
                validationErrorCode,
                isValidationPassed,
                isOtpValid,
                isUserFound,
                isPasswordUpdated,
                otpErrorCode);

            return errorResponse ?? CreateSuccessResponse();
        }

        /// <summary>
        /// Create error response based on validation flags.
        /// </summary>
        private ApiResponse<object>? CreateErrorResponse(
            string? validationErrorCode,
            bool isValidationPassed,
            bool isOtpValid,
            bool isUserFound,
            bool isPasswordUpdated,
            string? otpErrorCode)
        {
            if (!isValidationPassed)
            {
                return ApiResponse<object>.FailWithNull(validationErrorCode!);
            }
            if (!isOtpValid)
            {
                return ApiResponse<object>.FailWithNull(otpErrorCode!);
            }
            if (!isUserFound)
            {
                return ApiResponse<object>.FailWithNull(GeneralCode.APP_MESSAGE_4020.ToString());
            }
            if (!isPasswordUpdated)
            {
                return ApiResponse<object>.FailWithNull(GeneralCode.APP_MESSAGE_5001.ToString());
            }
            return null;
        }

        /// <summary>
        /// Create success response.
        /// </summary>
        private ApiResponse<object> CreateSuccessResponse()
        {
            return ApiResponse<object>.Success(GeneralCode.APP_MESSAGE_2000.ToString(), null);
        }
    }
}
