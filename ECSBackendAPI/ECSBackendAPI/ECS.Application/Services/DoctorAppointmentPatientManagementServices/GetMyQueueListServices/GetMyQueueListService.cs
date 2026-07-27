using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.GetMyQueueListServices
{
    /// <summary>
    /// Service for retrieving the queue list of the currently authenticated doctor.
    /// Automatically resolves the doctor profile from the JWT token user ID.
    /// </summary>
    public class GetMyQueueListService : IGetMyQueueListService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AppDbContext _context;

        /// <summary>
        /// Initializes a new instance of the service with required dependencies.
        /// </summary>
        /// <param name="httpContextAccessor">Provides access to the HTTP context for extracting user claims from the JWT token.</param>
        /// <param name="context">The application database context for querying queue and doctor profile data.</param>
        public GetMyQueueListService(
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        /// <summary>
        /// Processes the request to retrieve the queue list for the current doctor on the specified date.
        /// </summary>
        /// <param name="date">The date for which to retrieve the queue list.</param>
        /// <returns>An API response containing the queue list data or an error code on failure.</returns>
        public async Task<ApiResponse<GetMyQueueListResponse>> Process(DateOnly date)
        {
            try
            {
                var state = new ExecutionState();
                await ExtractAndValidateUserAsync(state);
                await ResolveDoctorProfileAsync(state);
                await GetQueueDataAsync(state.DoctorId, date, state);
                return CreateResponse(state, date);
            }
            catch (Exception)
            {
                return ApiResponse<GetMyQueueListResponse>.Fail(GeneralCode.APP_MESSAGE_5001.ToString());
            }
        }

        /// <summary>
        /// Holds all mutable execution state for the process flow.
        /// Tracks user authentication, doctor resolution, data loading, and error status.
        /// </summary>
        private class ExecutionState
        {
            /// <summary>The resolved doctor profile ID.</summary>
            public Guid DoctorId { get; set; } = Guid.Empty;

            /// <summary>Indicates whether the user is authenticated via JWT token.</summary>
            public bool IsUserAuthenticated { get; set; } = false;

            /// <summary>Indicates whether the doctor profile exists and is active.</summary>
            public bool IsDoctorExists { get; set; } = true;

            /// <summary>Indicates whether an error occurred during processing.</summary>
            public bool HasError { get; set; } = false;

            /// <summary>Indicates whether the queue data has been successfully loaded.</summary>
            public bool IsDataLoaded { get; set; } = false;

            /// <summary>The error code to return if an error occurred.</summary>
            public string? ErrorCode { get; set; }

            /// <summary>The list of queue entities retrieved from the database.</summary>
            public List<Queue> Queues { get; set; } = new();
        }

        /// <summary>
        /// Extracts the user ID from the JWT token claims and validates that a claim is present.
        /// </summary>
        /// <param name="state">The execution state to populate with user information.</param>
        private Task ExtractAndValidateUserAsync(ExecutionState state)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                state.IsUserAuthenticated = false;
                state.DoctorId = Guid.Empty;
                return Task.CompletedTask;
            }

            var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            var userIdClaim = claim != null ? claim.Value : null;
            var isClaimValid = !string.IsNullOrEmpty(userIdClaim);
            state.IsUserAuthenticated = isClaimValid;

            Guid.TryParse(userIdClaim, out var userId);
            state.DoctorId = userId;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Resolves the doctor profile from the user ID extracted from the token.
        /// Updates the state with the resolved doctor ID or sets an error if not found.
        /// </summary>
        /// <param name="state">The execution state containing the user ID to resolve.</param>
        private async Task ResolveDoctorProfileAsync(ExecutionState state)
        {
            var isUserValid = state.IsUserAuthenticated && state.DoctorId != Guid.Empty;
            if (!isUserValid)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4011.ToString();
                return;
            }

            var doctorProfile = await _context.Set<DoctorProfile>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == state.DoctorId && x.IsActive);

            if (doctorProfile == null)
            {
                state.IsDoctorExists = false;
                state.DoctorId = Guid.Empty;
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4011.ToString();
                return;
            }

            state.IsDoctorExists = true;
            state.DoctorId = doctorProfile.Id;
            state.HasError = false;
            state.ErrorCode = null;
        }

        /// <summary>
        /// Retrieves the queue data for the specified doctor and date from the database.
        /// Eager loads related appointment, patient, service, and room entities.
        /// </summary>
        /// <param name="doctorId">The doctor profile ID to filter queues.</param>
        /// <param name="date">The date for which to retrieve queues.</param>
        /// <param name="state">The execution state to populate with retrieved queue data.</param>
        private async Task GetQueueDataAsync(Guid doctorId, DateOnly date, ExecutionState state)
        {
            state.IsDataLoaded = false;
            var targetDate = date.ToDateTime(TimeOnly.MinValue);

            var queues = await _context.Set<Queue>()
                .AsNoTracking()
                .Include(q => q.Appointment)
                    .ThenInclude(a => a!.Patient)
                .Include(q => q.Appointment)
                    .ThenInclude(a => a!.Service)
                .Include(q => q.Appointment)
                    .ThenInclude(a => a!.MedicalRecord)
                .Include(q => q.Appointment)
                    .ThenInclude(a => a!.PreliminaryDiagnosis)
                .Include(q => q.Room)
                .Where(q => q.Appointment != null &&
                           q.Appointment.DoctorId == doctorId &&
                           q.Appointment.AppointmentDate.Date == targetDate.Date)
                .OrderBy(q => q.QueueNumber)
                .ToListAsync();

            state.Queues = queues;
            state.IsDataLoaded = true;
        }

        /// <summary>
        /// Creates the API response based on the execution state.
        /// Builds the response with statistics and mapped queue items.
        /// </summary>
        /// <param name="state">The execution state containing all processing results.</param>
        /// <param name="date">The date of the queue list.</param>
        /// <returns>A success or failure API response with the queue list data.</returns>
        private ApiResponse<GetMyQueueListResponse> CreateResponse(ExecutionState state, DateOnly date)
        {
            if (state.HasError || !state.IsDataLoaded)
            {
                return ApiResponse<GetMyQueueListResponse>.Fail(state.ErrorCode!);
            }

            var waitingCount = 0;
            var inProgressCount = 0;
            var completedCount = 0;
            var queueItems = new List<MyQueueItemDto>();

            foreach (var queue in state.Queues)
            {
                var item = MapToQueueItem(queue);
                queueItems.Add(item);
                CountByStatus(queue.Status, ref waitingCount, ref inProgressCount, ref completedCount);
            }

            var response = new GetMyQueueListResponse
            {
                Date = date,
                TotalPatients = state.Queues.Count,
                WaitingCount = waitingCount,
                InProgressCount = inProgressCount,
                CompletedCount = completedCount,
                Items = queueItems
            };

            return ApiResponse<GetMyQueueListResponse>.Success(GeneralCode.APP_MESSAGE_2001.ToString(), response);
        }

        /// <summary>
        /// Counts queue items by their status using flag-based comparisons.
        /// Updates the reference parameters for waiting, in-progress, and completed counts.
        /// </summary>
        /// <param name="status">The queue status to evaluate.</param>
        /// <param name="waiting">Reference to the waiting count accumulator.</param>
        /// <param name="inProgress">Reference to the in-progress count accumulator.</param>
        /// <param name="completed">Reference to the completed count accumulator.</param>
        private static void CountByStatus(QueueStatus status, ref int waiting, ref int inProgress, ref int completed)
        {
            switch (status)
            {
                case QueueStatus.WAITING:
                    waiting++;
                    break;
                case QueueStatus.CALLING:
                    inProgress++;
                    break;
                case QueueStatus.COMPLETED:
                    completed++;
                    break;
            }
        }

        /// <summary>
        /// Maps a queue entity to its corresponding DTO for API response.
        /// Extracts related patient, service, and room information.
        /// </summary>
        /// <param name="queue">The queue entity to map.</param>
        /// <returns>A mapped DTO representing the queue item.</returns>
        private static MyQueueItemDto MapToQueueItem(Queue queue)
        {
            var appointment = queue.Appointment!;
            var patient = appointment.Patient;
            var service = appointment.Service;
            var room = queue.Room;

            var patientName = !string.IsNullOrEmpty(patient.FullName) ? patient.FullName : "Unknown";
            var bookingSource = !string.IsNullOrEmpty(appointment.BookingSource) ? appointment.BookingSource : "UNKNOWN";

            var roomId = room != null ? (Guid?)room.Id : null;
            var roomName = room != null ? room.RoomName : null;
            var serviceName = service != null ? service.ServiceName : null;

            return new MyQueueItemDto
            {
                QueueId = queue.Id,
                QueueNumber = queue.QueueNumber,
                AppointmentId = queue.AppointmentId,
                PatientId = appointment.PatientId,
                PatientName = patientName,
                PatientPhone = patient.PhoneNumber,
                PatientDateOfBirth = patient.Dob,
                PatientGender = patient.Gender.ToString(),
                AppointmentTime = appointment.AppointmentDate,
                Symptoms = appointment.Symptoms,
                RoomId = roomId,
                RoomName = roomName,
                Status = queue.Status,
                StatusText = GetStatusText(queue.Status),
                CalledAt = queue.CalledAt,
                CompletedAt = queue.CompletedAt,
                HasMedicalRecord = appointment.MedicalRecord != null,
                HasPreliminaryDiagnosis = appointment.PreliminaryDiagnosis != null,
                ServiceName = serviceName,
                BookingSource = bookingSource
            };
        }

        /// <summary>
        /// Gets the localized display text for a queue status.
        /// </summary>
        /// <param name="status">The queue status enum value.</param>
        /// <returns>The Vietnamese display text for the status.</returns>
        private static string GetStatusText(QueueStatus status)
        {
            return status switch
            {
                QueueStatus.WAITING => "Đang chờ",
                QueueStatus.CALLING => "Đang khám",
                QueueStatus.COMPLETED => "Đã khám xong",
                _ => status.ToString()
            };
        }
    }
}
