using ECS.Application.Common.Response;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.AuthServices.ViewAccountInfoServices
{
    /// <summary>
    /// Handles the view account info business logic for the authenticated user.
    /// </summary>
    public class ViewAccountInfoService : IViewAccountInfoService
    {
        private readonly IRepositoryQueryBase<User, Guid, AppDbContext> _userRepository;
        private readonly IValidator<ViewAccountInfoRequest> _validator;

        /// <summary>
        /// Initializes a new instance of <see cref="ViewAccountInfoService"/> with required dependencies.
        /// </summary>
        /// <param name="userRepository">Repository for querying user data.</param>
        /// <param name="validator">Validator for view account info request data.</param>
        public ViewAccountInfoService(
            IRepositoryQueryBase<User, Guid, AppDbContext> userRepository,
            IValidator<ViewAccountInfoRequest> validator)
        {
            _userRepository = userRepository;
            _validator = validator;
        }

        /// <summary>
        /// Processes the view account info request by validating the user id and returning account details.
        /// </summary>
        /// <param name="viewAccountInfoRequest">The request containing the user id from JWT.</param>
        /// <returns>An <see cref="ApiResponse{ViewAccountInfoResponse}"/> containing account details or an error code.</returns>
        public async Task<ApiResponse<ViewAccountInfoResponse>> Process(ViewAccountInfoRequest viewAccountInfoRequest)
        {
            // Initialize status tracking flags
            bool isValidationPassed = true;
            bool isUserFound = true;
            string? validationErrorCode = null;
            // Step 1: Validate request data format
            ValidateRequest(viewAccountInfoRequest, ref isValidationPassed, ref validationErrorCode);
            // Step 2: Retrieve user by id
            var retrievedUser = await RetrieveUserData(viewAccountInfoRequest.UserId);
            // Step 3: Verify user existence
            ValidateUser(retrievedUser, ref isUserFound, isValidationPassed);
            // Step 4: Assemble API payload or generate error response
            return CreateResponse(retrievedUser, isValidationPassed, isUserFound, validationErrorCode);
        }

        /// <summary>
        /// Validates the incoming request payload against defined business rules.
        /// </summary>
        /// <param name="viewAccountInfoRequest">The request payload to validate.</param>
        /// <param name="isValidationPassed">Flag updated to <c>false</c> if validation fails.</param>
        /// <param name="validationErrorCode">Stores the first validation error code encountered.</param>
        private void ValidateRequest(
            ViewAccountInfoRequest viewAccountInfoRequest,
            ref bool isValidationPassed,
            ref string? validationErrorCode)
        {
            var result = _validator.Validate(viewAccountInfoRequest);
            if (!result.IsValid)
            {
                isValidationPassed = false;
                validationErrorCode = result.Errors.First().ErrorCode;
            }
        }

        /// <summary>
        /// Validates the retrieved user existence.
        /// </summary>
        /// <param name="retrievedUser">The user entity retrieved from the database, or <c>null</c> if not found.</param>
        /// <param name="isUserFound">Flag updated to <c>false</c> if user is not found.</param>
        /// <param name="isValidationPassed">Precondition flag indicating if request validation succeeded.</param>
        private void ValidateUser(
            User? retrievedUser,
            ref bool isUserFound,
            bool isValidationPassed)
        {
            if (!isValidationPassed)
            {
                return;
            }
            if (retrievedUser == null)
            {
                isUserFound = false;
            }
        }

        /// <summary>
        /// Queries the repository for an active user matching the given id.
        /// </summary>
        /// <param name="userId">The user id to search for.</param>
        /// <returns>The matching <see cref="User"/> if found and active; otherwise <c>null</c>.</returns>
        private async Task<User?> RetrieveUserData(Guid userId)
        {
            return await _userRepository
                .FindByCondition(x => x.Id == userId && x.IsActive)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Generates an API response payload based on validation status flags.
        /// </summary>
        /// <param name="retrievedUser">The resolved user entity instance.</param>
        /// <param name="isValidationPassed">Indicates whether request format validation succeeded.</param>
        /// <param name="isUserFound">Indicates whether the user was located.</param>
        /// <param name="validationErrorCode">The error code from validation failure, if any.</param>
        /// <returns>A configured <see cref="ApiResponse{ViewAccountInfoResponse}"/>.</returns>
        private ApiResponse<ViewAccountInfoResponse> CreateResponse(
            User? retrievedUser,
            bool isValidationPassed,
            bool isUserFound,
            string? validationErrorCode)
        {
            var errorResponse = CreateErrorResponse(
                isValidationPassed, isUserFound, validationErrorCode);
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
        /// <param name="isUserFound">Indicates whether the user was located.</param>
        /// <param name="validationErrorCode">The error code from validation failure.</param>
        /// <returns>A failed <see cref="ApiResponse{ViewAccountInfoResponse}"/> variant if errors are found; otherwise <c>null</c>.</returns>
        private ApiResponse<ViewAccountInfoResponse>? CreateErrorResponse(
            bool isValidationPassed,
            bool isUserFound,
            string? validationErrorCode)
        {
            if (!isValidationPassed)
            {
                return ApiResponse<ViewAccountInfoResponse>.Fail(validationErrorCode!);
            }
            if (!isUserFound)
            {
                return ApiResponse<ViewAccountInfoResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4020.ToString());
            }
            return null;
        }

        /// <summary>
        /// Creates a success response containing the account information.
        /// </summary>
        /// <param name="user">The authenticated user entity.</param>
        /// <returns>A success <see cref="ApiResponse{ViewAccountInfoResponse}"/> with the account payload.</returns>
        private ApiResponse<ViewAccountInfoResponse> CreateSuccessResponse(User user)
        {
            var response = new ViewAccountInfoResponse
            {
                Id = user.Id,
                Email = user.Email,
                Phone = user.Phone,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
            return ApiResponse<ViewAccountInfoResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }
    }
}
