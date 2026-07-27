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
        /// Coordinates the four-step account lookup workflow: request validation, active-user retrieval,
        /// user-existence evaluation, and API response construction.
        /// </summary>
        /// <param name="viewAccountInfoRequest">The request containing the user id from JWT.</param>
        /// <returns>An <see cref="ApiResponse{ViewAccountInfoResponse}"/> containing account details or an error code.</returns>
        public async Task<ApiResponse<ViewAccountInfoResponse>> Process(
            ViewAccountInfoRequest viewAccountInfoRequest)
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
        /// Validates the request and writes the resulting pass/fail state and validation code
        /// into the workflow state supplied by <see cref="Process"/>.
        /// </summary>
        /// <param name="viewAccountInfoRequest">The request payload to validate.</param>
        /// <param name="isValidationPassed">The workflow flag updated with the validation result.</param>
        /// <param name="validationErrorCode">The first validation error code, when validation fails.</param>
        private void ValidateRequest(
            ViewAccountInfoRequest viewAccountInfoRequest,
            ref bool isValidationPassed,
            ref string? validationErrorCode)
        {
            var result = _validator.Validate(viewAccountInfoRequest);
            var validationState = result.IsValid switch
            {
                true => (IsPassed: true, ErrorCode: (string?)null),
                false => (IsPassed: false, ErrorCode: result.Errors.First().ErrorCode)
            };

            isValidationPassed = validationState.IsPassed;
            validationErrorCode = validationState.ErrorCode;
        }

        /// <summary>
        /// Queries the repository for the active user identified by the request.
        /// </summary>
        /// <param name="userId">The user identifier extracted from the request.</param>
        /// <returns>The matching active user, or <see langword="null"/> when no match exists.</returns>
        private async Task<User?> RetrieveUserData(Guid userId)
        {
            return await _userRepository
                .FindByCondition(x => x.Id == userId && x.IsActive)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Resolves whether the retrieved user should be treated as found for the workflow.
        /// Invalid requests preserve the initial found state so response selection remains
        /// controlled by the validation result.
        /// </summary>
        /// <param name="retrievedUser">The active user returned by the repository.</param>
        /// <param name="isUserFound">The workflow flag updated with the user-existence result.</param>
        /// <param name="isValidationPassed">The request validation state.</param>
        private static void ValidateUser(
            User? retrievedUser,
            ref bool isUserFound,
            bool isValidationPassed)
        {
            isUserFound = (isValidationPassed, retrievedUser) switch
            {
                (false, _) => true,
                (true, null) => false,
                _ => true
            };
        }

        /// <summary>
        /// Selects the API response variant from validation and user-existence state.
        /// </summary>
        /// <param name="retrievedUser">The user used to build a successful response.</param>
        /// <param name="isValidationPassed">Whether request validation succeeded.</param>
        /// <param name="isUserFound">Whether an active user was retrieved.</param>
        /// <param name="validationErrorCode">The validation error code, when applicable.</param>
        /// <returns>A validation error, user-not-found error, or mapped success response.</returns>
        private static ApiResponse<ViewAccountInfoResponse> CreateResponse(
            User? retrievedUser,
            bool isValidationPassed,
            bool isUserFound,
            string? validationErrorCode)
        {
            return (isValidationPassed, isUserFound) switch
            {
                (false, _) => ApiResponse<ViewAccountInfoResponse>.Fail(validationErrorCode!),
                (true, false) => ApiResponse<ViewAccountInfoResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4020.ToString()),
                _ => CreateSuccessResponse(retrievedUser!)
            };
        }

        /// <summary>
        /// Maps the user entity into the account-information payload and wraps it in a success response.
        /// </summary>
        /// <param name="user">The active user entity to map.</param>
        /// <returns>A success response containing the mapped account information.</returns>
        private static ApiResponse<ViewAccountInfoResponse> CreateSuccessResponse(User user)
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
