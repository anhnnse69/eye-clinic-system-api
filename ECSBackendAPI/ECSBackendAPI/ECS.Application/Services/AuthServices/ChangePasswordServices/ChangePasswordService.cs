using ECS.Application.Common.Response;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.Helper.Utility;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.AuthServices.ChangePasswordServices
{
    /// <summary>
    /// Handles the change password business logic for authenticated users.
    /// </summary>
    public class ChangePasswordService : IChangePasswordService
    {
        private readonly IRepositoryBaseAsync<User, Guid, AppDbContext> _repository;
        private readonly IValidator<ChangePasswordRequest> _validator;

        /// <summary>
        /// Initializes a new instance of <see cref="ChangePasswordService"/> with required dependencies.
        /// </summary>
        /// <param name="repository">Repository used to query and update user data.</param>
        /// <param name="validator">Validator for change password request data.</param>
        public ChangePasswordService(
            IRepositoryBaseAsync<User, Guid, AppDbContext> repository,
            IValidator<ChangePasswordRequest> validator)
        {
            _repository = repository;
            _validator = validator;
        }

        /// <summary>
        /// Processes the change password request by validating input and updating the password hash.
        /// </summary>
        /// <param name="userId">The user id from JWT.</param>
        /// <param name="changePasswordRequest">The request containing current and new password.</param>
        /// <returns>An <see cref="ApiResponse{ChangePasswordResponse}"/> containing result or an error code.</returns>
        public async Task<ApiResponse<ChangePasswordResponse>> Process(Guid userId, ChangePasswordRequest changePasswordRequest)
        {
            bool isValidationPassed = true;
            bool isCurrentPasswordCorrect = true;
            string? validationErrorCode = null;

            ValidateRequest(changePasswordRequest, ref isValidationPassed, ref validationErrorCode);

            var retrievedUser = await RetrieveUserData(userId);

            ValidateCurrentPassword(retrievedUser, changePasswordRequest, ref isCurrentPasswordCorrect, isValidationPassed);

            return await CreateResponse(retrievedUser, isValidationPassed, isCurrentPasswordCorrect, validationErrorCode, changePasswordRequest);
        }

        /// <summary>
        /// Validates the incoming request payload against defined business rules.
        /// </summary>
        private void ValidateRequest(
            ChangePasswordRequest changePasswordRequest,
            ref bool isValidationPassed,
            ref string? validationErrorCode)
        {
            var result = _validator.Validate(changePasswordRequest);
            if (!result.IsValid)
            {
                isValidationPassed = false;
                validationErrorCode = result.Errors.First().ErrorCode;
            }
        }

        /// <summary>
        /// Validates that the current password matches the stored hash.
        /// </summary>
        private void ValidateCurrentPassword(
            User? retrievedUser,
            ChangePasswordRequest changePasswordRequest,
            ref bool isCurrentPasswordCorrect,
            bool isValidationPassed)
        {
            if (!isValidationPassed)
            {
                return;
            }
            if (retrievedUser == null)
            {
                isCurrentPasswordCorrect = false;
                return;
            }

            var isVerified = PasswordHelper.VerifyPassword(
                changePasswordRequest.CurrentPassword, retrievedUser.PasswordHash);
            if (!isVerified)
            {
                isCurrentPasswordCorrect = false;
            }
        }

        /// <summary>
        /// Queries the database for an active user matching the given id.
        /// </summary>
        private async Task<User?> RetrieveUserData(Guid userId)
        {
            return await _repository
                .FindByCondition(x => x.Id == userId && x.IsActive)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Generates an API response payload based on validation status flags.
        /// </summary>
        private async Task<ApiResponse<ChangePasswordResponse>> CreateResponse(
            User? retrievedUser,
            bool isValidationPassed,
            bool isCurrentPasswordCorrect,
            string? validationErrorCode,
            ChangePasswordRequest changePasswordRequest)
        {
            var errorResponse = CreateErrorResponse(
                isValidationPassed, isCurrentPasswordCorrect, validationErrorCode);
            if (errorResponse != null)
            {
                return errorResponse;
            }
            return await CreateSuccessResponse(retrievedUser!, changePasswordRequest);
        }

        /// <summary>
        /// Creates an error response based on validation failure flags.
        /// </summary>
        private ApiResponse<ChangePasswordResponse>? CreateErrorResponse(
            bool isValidationPassed,
            bool isCurrentPasswordCorrect,
            string? validationErrorCode)
        {
            if (!isValidationPassed)
            {
                return ApiResponse<ChangePasswordResponse>.Fail(validationErrorCode!);
            }
            if (!isCurrentPasswordCorrect)
            {
                return ApiResponse<ChangePasswordResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4039.ToString());
            }
            return null;
        }

        /// <summary>
        /// Creates a success response containing the updated account information.
        /// </summary>
        private async Task<ApiResponse<ChangePasswordResponse>> CreateSuccessResponse(User user, ChangePasswordRequest changePasswordRequest)
        {
            user.PasswordHash = PasswordHelper.HashPassword(changePasswordRequest.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(user);
            await _repository.SaveChangesAsync();

            return ApiResponse<ChangePasswordResponse>.Success(
                GeneralCode.APP_MESSAGE_2008.ToString(),
                new ChangePasswordResponse
                {
                    IsSuccess = true,
                    Message = GeneralCode.APP_MESSAGE_2008.ToString()
                });
        }
    }
}
