using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Feedbacks;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ClinicAdminManagementServices.DeleteClinicFeedbackServices
{
    /// <summary>
    /// Handles business operations responsible for soft deleting feedback records
    /// associated with the authenticated clinic administrator.
    /// </summary>
    public class DeleteClinicFeedbackService : IDeleteClinicFeedbackService
    {
        private readonly IRepositoryBaseAsync<Feedback, Guid, AppDbContext> _feedbackRepository;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteClinicFeedbackService"/> class
        /// with required repository boundaries and authentication context providers.
        /// </summary>
        /// <param name="feedbackRepository">
        /// Repository boundary instance responsible for managing feedback persistence operations.
        /// </param>
        /// <param name="staffClinicRepository">
        /// Repository boundary instance providing staff-to-clinic relationship query capabilities.
        /// </param>
        /// <param name="httpContextAccessor">
        /// Accessor used to retrieve authenticated user identity information from active HTTP pipelines.
        /// </param>
        public DeleteClinicFeedbackService(
            IRepositoryBaseAsync<Feedback, Guid, AppDbContext> feedbackRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _feedbackRepository = feedbackRepository;
            _staffClinicRepository = staffClinicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes the complete feedback deletion workflow including identity validation,
        /// clinic ownership verification, feedback existence checks,
        /// and persistence of soft delete modifications.
        /// </summary>
        /// <param name="request">
        /// The request payload containing the target feedback identifier.
        /// </param>
        /// <returns>
        /// An <see cref="ApiResponse{Boolean}"/> describing execution status
        /// and deletion outcome information.
        /// </returns>
        public async Task<ApiResponse<bool>> Process(DeleteClinicFeedbackRequest request)
        {
            // Initialize status tracking flags
            bool isUserValid = true;
            bool isClinicExist = true;
            bool isFeedbackExist = true;
            bool isAuthorizedFeedback = true;

            // Step 1: Extract identity information parameter metrics from active security claim session context
            var userId = RetrieveUserId(ref isUserValid);

            // Step 2: Search operational relational databases to identify clinic bound tightly to active account
            var staffClinic = await RetrieveClinicData(userId, isUserValid);

            // Step 3: Track context validation parameters safely before hitting core processing pipelines
            ValidateClinicContext(staffClinic, isUserValid, ref isClinicExist);

            // Step 4: Retrieve target feedback entity
            var feedback = await RetrieveFeedback(request.FeedbackId, isClinicExist);

            ValidateFeedbackExistence(feedback, ref isFeedbackExist);

            // Step 5: Validate ownership relationship between feedback and clinic
            ValidateFeedbackOwnership(feedback, staffClinic, isFeedbackExist, isClinicExist, ref isAuthorizedFeedback);

            // Step 6: Apply modifications to entity (Soft delete bằng cách chuyển IsPublic = false)
            UpdateFeedbackStatus(feedback, isClinicExist, isFeedbackExist, isAuthorizedFeedback);

            // Step 7: Persist changes and package processing outcome
            return await CreateResponse(feedback, isUserValid, isClinicExist, isFeedbackExist, isAuthorizedFeedback);
        }

        /// <summary>
        /// Resolves the authenticated user identifier from active security claim collections.
        /// </summary>
        /// <param name="isUserValid">
        /// Validation flag updated when claim extraction or identifier parsing fails.
        /// </param>
        /// <returns>
        /// The authenticated user identifier if successfully resolved; otherwise an empty GUID.
        /// </returns>
        private Guid RetrieveUserId(ref bool isUserValid)
        {
            var userIdClaim = _httpContextAccessor
                .HttpContext?
                .User?
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                isUserValid = false;
                return Guid.Empty;
            }

            return userId;
        }

        /// <summary>
        /// Retrieves the clinic relationship mapping associated with the authenticated user account.
        /// </summary>
        /// <param name="userId">
        /// The identifier representing the current authenticated user.
        /// </param>
        /// <param name="isUserValid">
        /// Validation state indicating whether user identity resolution succeeded.
        /// </param>
        /// <returns>
        /// The corresponding staff-clinic relationship record if found; otherwise null.
        /// </returns>
        private async Task<StaffClinic?> RetrieveClinicData(Guid userId, bool isUserValid)
        {
            if (!isUserValid)
            {
                return null;
            }

            return await _staffClinicRepository
                .FindByCondition(x => x.UserId == userId && x.IsActive)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Evaluates whether the authenticated user is associated with a valid clinic context.
        /// </summary>
        /// <param name="staffClinic">
        /// The resolved clinic relationship entity associated with the current user.
        /// </param>
        /// <param name="isUserValid">
        /// Indicates whether user authentication information was successfully resolved.
        /// </param>
        /// <param name="isClinicExist">
        /// Flag updated when no valid clinic association can be located.
        /// </param>
        private void ValidateClinicContext(StaffClinic? staffClinic, bool isUserValid, ref bool isClinicExist)
        {
            if (!isUserValid)
            {
                return;
            }

            if (staffClinic == null)
            {
                isClinicExist = false;
            }
        }

        /// <summary>
        /// Retrieves the targeted feedback entity from persistence storage using the supplied identifier.
        /// </summary>
        /// <param name="feedbackId">
        /// The feedback identifier provided by the incoming request.
        /// </param>
        /// <param name="isClinicExist">
        /// Validation flag indicating whether clinic context verification succeeded.
        /// </param>
        /// <returns>
        /// The matching feedback entity if found and publicly visible; otherwise null.
        /// </returns>
        private async Task<Feedback?> RetrieveFeedback(string feedbackId, bool isClinicExist)
        {
            if (!isClinicExist)
            {
                return null;
            }

            if (!Guid.TryParse(feedbackId, out Guid parsedFeedbackId))
            {
                return null;
            }

            return await _feedbackRepository
                    .FindByCondition(x => x.Id == parsedFeedbackId && x.IsPublic, true)
                    .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Evaluates whether the targeted feedback record exists within the persistence layer.
        /// </summary>
        /// <param name="feedback">
        /// The feedback entity returned from the repository query operation.
        /// </param>
        /// <param name="isFeedbackExist">
        /// Flag updated when the requested feedback cannot be located.
        /// </param>
        private void ValidateFeedbackExistence(Feedback? feedback, ref bool isFeedbackExist)
        {
            if (feedback == null)
            {
                isFeedbackExist = false;
            }
        }

        /// <summary>
        /// Validates that the targeted feedback record belongs to the authenticated clinic context.
        /// </summary>
        /// <param name="feedback">
        /// The feedback entity selected for deletion processing.
        /// </param>
        /// <param name="staffClinic">
        /// The clinic relationship entity associated with the authenticated administrator.
        /// </param>
        /// <param name="isFeedbackExist">
        /// Indicates whether the feedback record was successfully located.
        /// </param>
        /// <param name="isClinicExist">
        /// Indicates whether the clinic relationship context is valid.
        /// </param>
        /// <param name="isAuthorizedFeedback">
        /// Flag updated when ownership validation fails between clinic and feedback.
        /// </param>
        private void ValidateFeedbackOwnership(
            Feedback? feedback,
            StaffClinic? staffClinic,
            bool isFeedbackExist,
            bool isClinicExist,
            ref bool isAuthorizedFeedback)
        {
            if (!isFeedbackExist || !isClinicExist || feedback == null || staffClinic == null)
            {
                return;
            }

            if (feedback.ClinicId != staffClinic.ClinicId)
            {
                isAuthorizedFeedback = false;
            }
        }

        /// <summary>
        /// Applies soft delete modifications by updating feedback visibility status flags.
        /// </summary>
        /// <param name="feedback">
        /// The target feedback entity selected for update operations.
        /// </param>
        /// <param name="isClinicExist">
        /// Indicates whether clinic validation checks succeeded.
        /// </param>
        /// <param name="isFeedbackExist">
        /// Indicates whether feedback existence validation succeeded.
        /// </param>
        /// <param name="isAuthorizedFeedback">
        /// Indicates whether ownership validation checks succeeded.
        /// </param>
        private static void UpdateFeedbackStatus(
            Feedback? feedback,
            bool isClinicExist,
            bool isFeedbackExist,
            bool isAuthorizedFeedback)
        {
            if (!isClinicExist || !isFeedbackExist || !isAuthorizedFeedback || feedback == null)
            {
                return;
            }
            feedback.IsPublic = false;
        }

        /// <summary>
        /// Coordinates validation outcome evaluation, persists entity modifications,
        /// and packages standardized API responses.
        /// </summary>
        /// <param name="feedback">
        /// The feedback entity containing pending update state modifications.
        /// </param>
        /// <param name="isUserValid">
        /// Indicates whether user identity validation succeeded.
        /// </param>
        /// <param name="isClinicExist">
        /// Indicates whether clinic context validation succeeded.
        /// </param>
        /// <param name="isFeedbackExist">
        /// Indicates whether feedback existence validation succeeded.
        /// </param>
        /// <param name="isAuthorizedFeedback">
        /// Indicates whether clinic ownership validation succeeded.
        /// </param>
        /// <returns>
        /// A standardized API response describing processing results.
        /// </returns>
        private async Task<ApiResponse<bool>> CreateResponse(
            Feedback? feedback,
            bool isUserValid,
            bool isClinicExist,
            bool isFeedbackExist,
            bool isAuthorizedFeedback)
        {
            var errorResponse = CreateErrorResponse(
                isUserValid,
                isClinicExist,
                isFeedbackExist,
                isAuthorizedFeedback);

            if (errorResponse != null)
            {
                return errorResponse;
            }

            await _feedbackRepository.UpdateAsync(feedback!);
            await _feedbackRepository.SaveChangesAsync();

            return ApiResponse<bool>.Success(
                GeneralCode.APP_MESSAGE_4037.ToString(),
                true);
        }

        /// <summary>
        /// Evaluates validation state variables and generates standardized error responses
        /// when business rule requirements are not satisfied.
        /// </summary>
        /// <param name="isUserValid">
        /// Indicates whether authentication information is valid.
        /// </param>
        /// <param name="isClinicExist">
        /// Indicates whether a clinic association exists for the authenticated user.
        /// </param>
        /// <param name="isFeedbackExist">
        /// Indicates whether the requested feedback record exists.
        /// </param>
        /// <param name="isAuthorizedFeedback">
        /// Indicates whether the feedback belongs to the authenticated clinic.
        /// </param>
        /// <returns>
        /// A failed API response when validation rules are violated; otherwise null.
        /// </returns>
        private ApiResponse<bool>? CreateErrorResponse(
            bool isUserValid,
            bool isClinicExist,
            bool isFeedbackExist,
            bool isAuthorizedFeedback)
        {
            if (!isUserValid)
            {
                return ApiResponse<bool>.Fail(
                    GeneralCode.APP_MESSAGE_4033.ToString());
            }

            if (!isClinicExist)
            {
                return ApiResponse<bool>.Fail(
                    GeneralCode.APP_MESSAGE_4034.ToString());
            }

            if (!isFeedbackExist)
            {
                return ApiResponse<bool>.Fail(
                    GeneralCode.APP_MESSAGE_4035.ToString());
            }

            if (!isAuthorizedFeedback)
            {
                return ApiResponse<bool>.Fail(
                    GeneralCode.APP_MESSAGE_4036.ToString());
            }

            return null;
        }
    }
}