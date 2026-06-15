using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Feedbacks;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ClinicDoctorDiscoveryService.ViewClinicFeedbacksServices
{
    /// <summary>
    /// Handles retrieving paginated feedbacks and rating
    /// summary for a clinic.
    /// </summary>
    public class ViewClinicFeedbacksService : IViewClinicFeedbacksService
    {
        private readonly IRepositoryQueryBase<
            Clinic,
            Guid,
            AppDbContext> _clinicRepository;

        private readonly IRepositoryQueryBase<
            Feedback,
            Guid,
            AppDbContext> _feedbackRepository;

        /// <summary>
        /// Initializes a new instance of
        /// <see cref="ViewClinicFeedbacksService"/>.
        /// </summary>
        public ViewClinicFeedbacksService(
            IRepositoryQueryBase<
                Clinic,
                Guid,
                AppDbContext> clinicRepository,
            IRepositoryQueryBase<
                Feedback,
                Guid,
                AppDbContext> feedbackRepository)
        {
            _clinicRepository = clinicRepository;
            _feedbackRepository = feedbackRepository;
        }

        /// <summary>
        /// Retrieves paginated feedbacks together with the
        /// clinic's overall rating summary.
        /// </summary>
        /// <param name="clinicId">
        /// Identifier of the clinic.
        /// </param>
        /// <param name="request">
        /// Pagination parameters.
        /// </param>
        /// <returns>
        /// A successful response containing the paginated
        /// feedbacks and rating summary.
        /// </returns>
        public async Task<ApiResponse<ViewClinicFeedbacksResponse>> Process(
            Guid clinicId,
            ViewClinicFeedbacksRequest request)
        {
            var clinic = await GetClinicOrThrowAsync(clinicId);
            var (pageNumber, pageSize) = NormalizePaging(request);
            var totalRecords = await CountFeedbacksAsync(clinicId);
            var totalPages = CalculateTotalPages(totalRecords, pageSize);
            var feedbacks = await FetchFeedbacksAsync(clinicId, pageNumber, pageSize);
            var response = BuildResponse(clinic, feedbacks, pageNumber, pageSize, totalPages, totalRecords);
            return CreateSuccessResponse(response);
        }

        /// <summary>
        /// Retrieves an active clinic by identifier.
        /// Throws an exception when the clinic does not exist
        /// or has been deactivated.
        /// </summary>
        /// <param name="clinicId">
        /// Identifier of the clinic.
        /// </param>
        /// <returns>
        /// The clinic entity.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the clinic cannot be found.
        /// </exception>
        private async Task<Clinic> GetClinicOrThrowAsync(
            Guid clinicId)
        {
            var clinic = await _clinicRepository
                .FindByCondition(c =>
                    c.Id == clinicId &&
                    c.IsActive)
                .FirstOrDefaultAsync();
            return clinic
                ?? throw new KeyNotFoundException(
                    GeneralCode.APP_MESSAGE_4008.ToString());
        }

        /// <summary>
        /// Normalizes the pagination parameters,
        /// ensuring valid minimum values.
        /// </summary>
        /// <param name="request">
        /// Pagination request.
        /// </param>
        /// <returns>
        /// A tuple containing the normalized page number
        /// and page size.
        /// </returns>
        private static (int PageNumber, int PageSize) NormalizePaging(
            ViewClinicFeedbacksRequest request)
        {
            var pageNumber = request.PageNumber < 1
                ? 1
                : request.PageNumber;
            var pageSize = request.PageSize < 1
                ? 10
                : request.PageSize;
            return (pageNumber, pageSize);
        }

        /// <summary>
        /// Counts the total number of public feedbacks
        /// belonging to the clinic.
        /// </summary>
        /// <param name="clinicId">
        /// Clinic identifier.
        /// </param>
        /// <returns>
        /// Total number of feedback records.
        /// </returns>
        private async Task<int> CountFeedbacksAsync(
            Guid clinicId)
        {
            return await _feedbackRepository
                .FindByCondition(f =>
                    f.ClinicId == clinicId &&
                    f.IsPublic)
                .CountAsync();
        }

        /// <summary>
        /// Calculates the total number of pages
        /// based on total records and page size.
        /// </summary>
        /// <param name="totalRecords">
        /// Total number of records.
        /// </param>
        /// <param name="pageSize">
        /// Number of records per page.
        /// </param>
        /// <returns>
        /// Total number of pages.
        /// </returns>
        private static int CalculateTotalPages(
            int totalRecords,
            int pageSize)
        {
            return pageSize == 0
                ? 0
                : (int)Math.Ceiling(
                    (double)totalRecords / pageSize);
        }

        /// <summary>
        /// Retrieves a page of public feedbacks
        /// for the specified clinic.
        /// </summary>
        /// <param name="clinicId">
        /// Clinic identifier.
        /// </param>
        /// <param name="pageNumber">
        /// Page number (1-based).
        /// </param>
        /// <param name="pageSize">
        /// Number of records per page.
        /// </param>
        /// <returns>
        /// Collection of feedback items.
        /// </returns>
        private async Task<List<ClinicFeedbackItem>> FetchFeedbacksAsync(
            Guid clinicId,
            int pageNumber,
            int pageSize)
        {
            return await _feedbackRepository
                .FindByCondition(f =>
                    f.ClinicId == clinicId &&
                    f.IsPublic)
                .OrderByDescending(f => f.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new ClinicFeedbackItem
                {
                    Id = f.Id,
                    PatientName = f.Patient.FullName,
                    RatingDoctor = f.RatingDoctor,
                    RatingClinic = f.RatingClinic,
                    Comment = f.Comment,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();
        }

        /// <summary>
        /// Builds the clinic feedbacks response object.
        /// </summary>
        /// <param name="clinic">
        /// Clinic entity.
        /// </param>
        /// <param name="feedbacks">
        /// Feedback items for the current page.
        /// </param>
        /// <param name="pageNumber">
        /// Current page number.
        /// </param>
        /// <param name="pageSize">
        /// Page size.
        /// </param>
        /// <param name="totalPages">
        /// Total number of pages.
        /// </param>
        /// <param name="totalRecords">
        /// Total number of records.
        /// </param>
        /// <returns>
        /// Clinic feedbacks response.
        /// </returns>
        private static ViewClinicFeedbacksResponse BuildResponse(
            Clinic clinic,
            List<ClinicFeedbackItem> feedbacks,
            int pageNumber,
            int pageSize,
            int totalPages,
            int totalRecords)
        {
            return new ViewClinicFeedbacksResponse
            {
                RatingAvg = clinic.RatingAvg,
                ReviewCount = clinic.ReviewCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                Feedbacks = feedbacks
            };
        }

        /// <summary>
        /// Creates a successful API response.
        /// </summary>
        /// <param name="response">
        /// Response payload.
        /// </param>
        /// <returns>
        /// Success API response.
        /// </returns>
        private static ApiResponse<ViewClinicFeedbacksResponse>
            CreateSuccessResponse(
                ViewClinicFeedbacksResponse response)
        {
            return ApiResponse<ViewClinicFeedbacksResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }
    }
}