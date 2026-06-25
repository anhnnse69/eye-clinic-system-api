using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Feedbacks;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.PatientAppointmentManagementServices.SubmitFeedbackServices
{
    /// <summary>
    /// Handles the business logic for submitting feedback and rating for completed appointments.
    /// </summary>
    public class SubmitFeedbackService : ISubmitFeedbackService
    {
        private readonly IRepositoryBaseAsync<Feedback, Guid, AppDbContext> _feedbackRepository;
        private readonly IRepositoryBaseAsync<Appointment, Guid, AppDbContext> _appointmentRepository;
        private readonly IRepositoryBaseAsync<DoctorProfile, Guid, AppDbContext> _doctorRepository;
        private readonly IRepositoryBaseAsync<Clinic, Guid, AppDbContext> _clinicRepository;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitFeedbackService"/> class with required infrastructure boundaries.
        /// </summary>
        /// <param name="feedbackRepository">Repository boundary instance for tracking persistent feedback changes.</param>
        /// <param name="appointmentRepository">Repository boundary instance for querying physical appointment records.</param>
        /// <param name="doctorRepository">Repository boundary instance for updating doctor rating.</param>
        /// <param name="clinicRepository">Repository boundary instance for updating clinic rating.</param>
        /// <param name="context">The underlying database persistence instance mapping multi-entity relational graph data models.</param>
        /// <param name="httpContextAccessor">Accessor to safely retrieve authentication claims identities out of current HTTP request pipelines.</param>
        public SubmitFeedbackService(
            IRepositoryBaseAsync<Feedback, Guid, AppDbContext> feedbackRepository,
            IRepositoryBaseAsync<Appointment, Guid, AppDbContext> appointmentRepository,
            IRepositoryBaseAsync<DoctorProfile, Guid, AppDbContext> doctorRepository,
            IRepositoryBaseAsync<Clinic, Guid, AppDbContext> clinicRepository,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _feedbackRepository = feedbackRepository;
            _appointmentRepository = appointmentRepository;
            _doctorRepository = doctorRepository;
            _clinicRepository = clinicRepository;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes the internal data pipeline workflow to validate access, create feedback, and update ratings.
        /// </summary>
        /// <param name="request">The parameters containing feedback data.</param>
        /// <returns>An <see cref="ApiResponse{SubmitFeedbackResponse}"/> enclosing descriptive data structures.</returns>
        public async Task<ApiResponse<SubmitFeedbackResponse>> Process(SubmitFeedbackRequest request)
        {
            // Initialize status tracking flags
            bool isUserValid = true;
            bool isAppointmentExist = true;
            bool isPermissionValid = true;
            bool isFeedbackValid = true;

            // Step 1: Extract identity information metrics from active token pipelines
            var userId = RetrieveUserId(ref isUserValid);

            // Step 2: Retrieve accessible profile IDs for the user
            var accessibleProfileIds = await RetrieveLinkedProfileIds(userId, isUserValid);

            // Step 3: Retrieve the appointment with necessary navigation properties
            var (appointment, appointmentExists) = await RetrieveAppointment(request.AppointmentId);
            isAppointmentExist = appointmentExists;

            // Step 4: Validate user permission and feedback eligibility
            (isPermissionValid, isFeedbackValid) = ValidateFeedbackEligibility(
                userId,
                accessibleProfileIds,
                appointment,
                isUserValid,
                isAppointmentExist,
                request);

            // Step 5: Process the feedback submission
            var response = await ProcessFeedbackSubmission(
                appointment!,
                request,
                isPermissionValid,
                isFeedbackValid);

            // Step 6: Package contextual payloads dynamically to manage outcome states
            return CreateResponse(response, isUserValid, isAppointmentExist, isPermissionValid, isFeedbackValid);
        }

        /// <summary>
        /// Resolves the logged-in user credentials via claims identity mapping streams.
        /// </summary>
        /// <param name="isUserValid">Guard state flag modified by reference to track authentication integrity.</param>
        /// <returns>The extracted structural global unique identifiers token block mapping the active user account context.</returns>
        private Guid RetrieveUserId(ref bool isUserValid)
        {
            var userIdClaim = _httpContextAccessor
                .HttpContext?
                .User
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
        /// Probes many-to-many link tables to extract authorized patient metrics identifiers.
        /// </summary>
        /// <param name="userId">The system reference credentials tracking structural context nodes.</param>
        /// <param name="isUserValid">Guard validation state assessing token evaluation integrity parameters.</param>
        /// <returns>A list tracking unique entity vector addresses mapping authorized relational profiles data blocks.</returns>
        private async Task<List<Guid>> RetrieveLinkedProfileIds(Guid userId, bool isUserValid)
        {
            if (!isUserValid) return new List<Guid>();

            var directProfileIds = await _context.Set<PatientProfile>()
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.Id)
                .ToListAsync();

            var linkedProfileIds = await _context.Set<UserPatient>()
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.PatientId)
                .ToListAsync();

            return directProfileIds.Union(linkedProfileIds).Distinct().ToList();
        }

        /// <summary>
        /// Retrieves the appointment by ID with necessary navigation properties.
        /// </summary>
        /// <param name="appointmentId">The appointment ID to retrieve.</param>
        /// <returns>A tuple containing the appointment entity and a flag indicating existence.</returns>
        private async Task<(Appointment? Appointment, bool Exists)> RetrieveAppointment(Guid appointmentId)
        {
            var appointment = await _appointmentRepository
                .FindByCondition(a => a.Id == appointmentId, trackChanges: false)
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Clinic)
                .Include(a => a.Feedback)
                .FirstOrDefaultAsync();

            if (appointment == null)
            {
                return (null, false);
            }

            return (appointment, true);
        }

        /// <summary>
        /// Validates whether the current user has permission to submit feedback and checks eligibility.
        /// </summary>
        /// <param name="userId">The authenticated user ID.</param>
        /// <param name="accessibleProfileIds">List of profile IDs the user can access.</param>
        /// <param name="appointment">The appointment entity.</param>
        /// <param name="isUserValid">Guard state monitoring authentication pipeline checkpoints.</param>
        /// <param name="isAppointmentExist">Guard state monitoring appointment existence.</param>
        /// <param name="request">The feedback request containing ratings.</param>
        /// <returns>A tuple containing permission validation flag and feedback validation flag.</returns>
        private (bool IsPermissionValid, bool IsFeedbackValid) ValidateFeedbackEligibility(
            Guid userId,
            List<Guid> accessibleProfileIds,
            Appointment? appointment,
            bool isUserValid,
            bool isAppointmentExist,
            SubmitFeedbackRequest request)
        {
            if (!isUserValid || !isAppointmentExist || appointment == null)
            {
                return (false, false);
            }

            // Step 1: Check if the user has access to this patient profile
            var hasAccess = accessibleProfileIds.Contains(appointment.PatientId)
                            || appointment.CreatedById == userId
                            || appointment.PatientId == userId;

            if (!hasAccess)
            {
                return (false, false);
            }

            // Step 2: Check if appointment is completed
            if (appointment.Status != AppointmentStatus.COMPLETED)
            {
                return (true, false);
            }

            // Step 3: Check if feedback already exists
            if (appointment.Feedback != null)
            {
                return (true, false);
            }

            // Step 4: Validate ratings (1-5)
            if (request.RatingDoctor < 1 || request.RatingDoctor > 5 ||
                request.RatingClinic < 1 || request.RatingClinic > 5)
            {
                return (true, false);
            }

            return (true, true);
        }

        /// <summary>
        /// Processes the feedback submission by creating feedback and updating ratings.
        /// </summary>
        /// <param name="appointment">The appointment entity.</param>
        /// <param name="request">The feedback request containing ratings and comment.</param>
        /// <param name="isPermissionValid">Guard state flag tracking permission validation.</param>
        /// <param name="isFeedbackValid">Guard state flag tracking feedback eligibility.</param>
        /// <returns>A response containing feedback details.</returns>
        private async Task<SubmitFeedbackResponse?> ProcessFeedbackSubmission(
            Appointment appointment,
            SubmitFeedbackRequest request,
            bool isPermissionValid,
            bool isFeedbackValid)
        {
            if (!isPermissionValid || !isFeedbackValid || appointment == null)
            {
                return null;
            }

            using var transaction = await _feedbackRepository.BeginTransactionAsync();

            try
            {
                // Step 1: Create feedback entity
                var feedback = new Feedback
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointment.Id,
                    PatientId = appointment.PatientId,
                    DoctorId = appointment.DoctorId,
                    ClinicId = appointment.Doctor.ClinicId,
                    RatingDoctor = request.RatingDoctor,
                    RatingClinic = request.RatingClinic,
                    Comment = request.Comment,
                    IsPublic = request.IsPublic,
                    CreatedAt = DateTime.Now
                };

                await _feedbackRepository.CreateAsync(feedback);

                // Step 2: Update appointment with feedback reference
                appointment.Feedback = feedback;
                appointment.UpdatedAt = DateTime.Now;
                await _appointmentRepository.UpdateAsync(appointment);

                // Step 3: Update doctor rating
                await UpdateDoctorRating(appointment.DoctorId);

                // Step 4: Update clinic rating
                await UpdateClinicRating(appointment.Doctor.ClinicId);

                // Step 5: Commit transaction
                await transaction.CommitAsync();

                // Step 6: Build response
                var response = new SubmitFeedbackResponse
                {
                    FeedbackId = feedback.Id.ToString(),
                    AppointmentId = appointment.Id.ToString(),
                    RatingDoctor = feedback.RatingDoctor,
                    RatingClinic = feedback.RatingClinic,
                    Comment = feedback.Comment,
                    IsPublic = feedback.IsPublic,
                    CreatedAt = feedback.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                };

                return response;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Updates the doctor's average rating and review count.
        /// </summary>
        /// <param name="doctorId">The doctor ID to update.</param>
        private async Task UpdateDoctorRating(Guid doctorId)
        {
            // Find doctor using GetByIdAsync from IRepositoryBaseAsync
            var doctor = await _doctorRepository.GetByIdAsync(doctorId);
            if (doctor == null) return;

            var feedbacks = await _context.Set<Feedback>()
                .Where(f => f.DoctorId == doctorId)
                .ToListAsync();

            if (feedbacks.Any())
            {
                doctor.RatingAvg = (decimal)feedbacks.Average(f => f.RatingDoctor);
                doctor.ReviewCount = feedbacks.Count;
            }
            else
            {
                doctor.RatingAvg = 0;
                doctor.ReviewCount = 0;
            }

            await _doctorRepository.UpdateAsync(doctor);
        }

        /// <summary>
        /// Updates the clinic's average rating and review count.
        /// </summary>
        /// <param name="clinicId">The clinic ID to update.</param>
        private async Task UpdateClinicRating(Guid clinicId)
        {
            // Find clinic using GetByIdAsync from IRepositoryBaseAsync
            var clinic = await _clinicRepository.GetByIdAsync(clinicId);
            if (clinic == null) return;

            var feedbacks = await _context.Set<Feedback>()
                .Where(f => f.ClinicId == clinicId)
                .ToListAsync();

            if (feedbacks.Any())
            {
                clinic.RatingAvg = (decimal)feedbacks.Average(f => f.RatingClinic);
                clinic.ReviewCount = feedbacks.Count;
            }
            else
            {
                clinic.RatingAvg = 0;
                clinic.ReviewCount = 0;
            }

            await _clinicRepository.UpdateAsync(clinic);
        }

        /// <summary>
        /// Resolves transaction outcome wrappers packing serialization nodes safely.
        /// </summary>
        /// <param name="response">The internal serializable structure returned out of core projection chains.</param>
        /// <param name="isUserValid">Guard context parameter evaluating token claim validity bounds.</param>
        /// <param name="isAppointmentExist">Guard monitoring parameter checking appointment existence.</param>
        /// <param name="isPermissionValid">Guard monitoring parameter checking permission boundaries.</param>
        /// <param name="isFeedbackValid">Guard monitoring parameter checking feedback eligibility.</param>
        /// <returns>A structured envelope holding operational response outcomes ready for presentation nodes.</returns>
        private ApiResponse<SubmitFeedbackResponse> CreateResponse(
            SubmitFeedbackResponse? response,
            bool isUserValid,
            bool isAppointmentExist,
            bool isPermissionValid,
            bool isFeedbackValid)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isAppointmentExist, isPermissionValid, isFeedbackValid);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<SubmitFeedbackResponse>.Success(
                GeneralCode.APP_MESSAGE_2009.ToString(),
                response!);
        }

        /// <summary>
        /// Evaluates functional exceptions sequences to render failure metadata nodes.
        /// </summary>
        /// <param name="isUserValid">Guard indicating whether authorization checkpoints cleared successfully.</param>
        /// <param name="isAppointmentExist">Guard indicating whether appointment exists.</param>
        /// <param name="isPermissionValid">Guard indicating whether permission validation cleared successfully.</param>
        /// <param name="isFeedbackValid">Guard indicating whether feedback eligibility checks passed.</param>
        /// <returns>A failure configuration block, or null if execution tracks meet standard benchmarks.</returns>
        private ApiResponse<SubmitFeedbackResponse>? CreateErrorResponse(
            bool isUserValid,
            bool isAppointmentExist,
            bool isPermissionValid,
            bool isFeedbackValid)
        {
            if (!isUserValid)
            {
                return ApiResponse<SubmitFeedbackResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4001.ToString());
            }

            if (!isAppointmentExist)
            {
                return ApiResponse<SubmitFeedbackResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4046.ToString());
            }

            if (!isPermissionValid)
            {
                return ApiResponse<SubmitFeedbackResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4053.ToString());
            }

            if (!isFeedbackValid)
            {
                return ApiResponse<SubmitFeedbackResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4055.ToString());
            }

            return null;
        }
    }
}