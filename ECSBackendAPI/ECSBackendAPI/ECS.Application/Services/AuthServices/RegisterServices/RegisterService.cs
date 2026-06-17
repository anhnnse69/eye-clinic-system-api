using ECS.Application.Common.Response;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.Helper.Utility;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.AuthServices.RegisterServices
{
    /// <summary>
    /// Handles user registration process.
    /// </summary>
    public class RegisterService : IRegisterService
    {
        private readonly IRepositoryQueryBase<User, Guid, AppDbContext>
            _userQueryRepository;
        private readonly IRepositoryBaseAsync<User, Guid, AppDbContext>
            _userRepository;
        private readonly IValidator<RegisterRequest> _validator;

        /// <summary>
        /// Initializes a new instance of <see cref="RegisterService"/> with required dependencies.
        /// </summary>
        /// <param name="userQueryRepository">Repository for querying user data.</param>
        /// <param name="userRepository">Repository for creating and persisting user entities.</param>
        /// <param name="validator">Validator for registration request data.</param>
        public RegisterService(
            IRepositoryQueryBase<User, Guid, AppDbContext> userQueryRepository,
            IRepositoryBaseAsync<User, Guid, AppDbContext> userRepository,
            IValidator<RegisterRequest> validator)
        {
            _userQueryRepository = userQueryRepository;
            _userRepository = userRepository;
            _validator = validator;
        }

        /// <summary>
        /// Processes the user registration request by validating input data, checking for duplicates, and creating a new user.
        /// </summary>
        /// <param name="request">The registration request containing user details.</param>
        /// <returns>An <see cref="ApiResponse{Object}"/> indicating success or an error code.</returns>
        public async Task<ApiResponse<object>> Process(RegisterRequest request)
        {
            // Step 1: Validate request data format
            var validationResult = ValidateRequest(request);
            // Step 2: Check for duplicate email
            var emailResult = await CheckEmailUniqueness(request, validationResult.IsPassed);
            // Step 3: Check for duplicate phone
            var phoneResult = await CheckPhoneUniqueness(request, validationResult.IsPassed);
            // Step 4: Assemble API payload or generate error response
            return await CreateResponse(validationResult, emailResult, phoneResult, request);
        }

        /// <summary>
        /// Validates the incoming request payload against defined business rules.
        /// </summary>
        /// <param name="request">The request payload to validate.</param>
        /// <returns>A validation result containing pass status and error code if failed.</returns>
        private ValidationResult ValidateRequest(RegisterRequest request)
        {
            var result = _validator.Validate(request);
            if (!result.IsValid)
            {
                return new ValidationResult(false, result.Errors.First().ErrorCode);
            }
            return new ValidationResult(true, null);
        }

        /// <summary>
        /// Checks if the provided email address is already registered in the system.
        /// </summary>
        /// <param name="request">The registration request containing the email to check.</param>
        /// <param name="isValidationPassed">Precondition flag indicating if request validation succeeded.</param>
        /// <returns>A result containing pass status.</returns>
        private async Task<UniquenessResult> CheckEmailUniqueness(
            RegisterRequest request,
            bool isValidationPassed)
        {
            if (!isValidationPassed)
            {
                return new UniquenessResult(true);
            }
            var normalizedEmail = request.Email.Trim().ToLower();
            var emailExists = await _userQueryRepository
                .FindByCondition(x =>
                    x.Email != null &&
                    x.Email.ToLower() == normalizedEmail)
                .AnyAsync();
            return new UniquenessResult(emailExists);
        }

        /// <summary>
        /// Checks if the provided phone number is already registered in the system.
        /// </summary>
        /// <param name="request">The registration request containing the phone to check.</param>
        /// <param name="isValidationPassed">Precondition flag indicating if request validation succeeded.</param>
        /// <returns>A result containing pass status.</returns>
        private async Task<UniquenessResult> CheckPhoneUniqueness(
            RegisterRequest request,
            bool isValidationPassed)
        {
            if (!isValidationPassed)
            {
                return new UniquenessResult(true);
            }
            var phoneExists = await _userQueryRepository
                .FindByCondition(x => x.Phone == request.Phone.Trim())
                .AnyAsync();
            return new UniquenessResult(phoneExists);
        }

        /// <summary>
        /// Generates an API response payload based on validation status flags.
        /// </summary>
        /// <param name="validationResult">The result of format validation.</param>
        /// <param name="emailResult">The result of email uniqueness check.</param>
        /// <param name="phoneResult">The result of phone uniqueness check.</param>
        /// <param name="request">The validated registration request.</param>
        /// <returns>A configured <see cref="ApiResponse{Boolean}"/>.</returns>
        private async Task<ApiResponse<object>> CreateResponse(
            ValidationResult validationResult,
            UniquenessResult emailResult,
            UniquenessResult phoneResult,
            RegisterRequest request)
        {
            var errorResponse = CreateErrorResponse(validationResult, emailResult, phoneResult);
            if (errorResponse != null)
            {
                return errorResponse;
            }
            return await CreateUserAsync(request);
        }

        /// <summary>
        /// Creates an error response based on validation failure flags.
        /// </summary>
        /// <param name="validationResult">The result of format validation.</param>
        /// <param name="emailResult">The result of email uniqueness check.</param>
        /// <param name="phoneResult">The result of phone uniqueness check.</param>
        /// <returns>A failed <see cref="ApiResponse{Boolean}"/> variant if errors are found; otherwise <c>null</c>.</returns>
        private ApiResponse<object>? CreateErrorResponse(
            ValidationResult validationResult,
            UniquenessResult emailResult,
            UniquenessResult phoneResult)
        {
            if (!validationResult.IsPassed)
            {
                return ApiResponse<object>.FailWithNull(validationResult.ErrorCode!);
            }
            if (emailResult.Exists)
            {
                return ApiResponse<object>.FailWithNull(
                    GeneralCode.APP_MESSAGE_4017.ToString());
            }
            if (phoneResult.Exists)
            {
                return ApiResponse<object>.FailWithNull(
                    GeneralCode.APP_MESSAGE_4018.ToString());
            }
            return null;
        }

        /// <summary>
        /// Persists the new user to the database and returns a success response.
        /// </summary>
        /// <param name="request">The validated registration request.</param>
        /// <returns>A success <see cref="ApiResponse{Boolean}"/> with value true.</returns>
        private async Task<ApiResponse<object>> CreateUserAsync(RegisterRequest request)
        {
            var user = BuildUser(request);
            await _userRepository.CreateAsync(user);
            await _userRepository.SaveChangesAsync();
            return ApiResponse<object>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(), true);
        }

        /// <summary>
        /// Builds a new <see cref="User"/> entity from the registration request.
        /// Role defaults to <see cref="UserRole.PATIENT"/>.
        /// </summary>
        /// <param name="request">The validated registration data.</param>
        /// <returns>A new <see cref="User"/> entity ready to be persisted.</returns>
        private User BuildUser(RegisterRequest request)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim().ToLower(),
                Phone = request.Phone.Trim(),
                PasswordHash = PasswordHelper.HashPassword(request.Password),
                Role = UserRole.PATIENT,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Represents the result of a format validation operation.
        /// </summary>
        private record ValidationResult(bool IsPassed, string? ErrorCode);

        /// <summary>
        /// Represents the result of a uniqueness check operation.
        /// </summary>
        private record UniquenessResult(bool Exists);
    }
}
