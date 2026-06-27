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
            catch (Exception ex)
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
            var userIdClaim = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

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

            var isDoctorFound = doctorProfile != null;
            state.IsDoctorExists = isDoctorFound;
            state.DoctorId = isDoctorFound ? doctorProfile!.Id : Guid.Empty;
            state.HasError = !isDoctorFound;
            state.ErrorCode = isDoctorFound ? null : GeneralCode.APP_MESSAGE_4011.ToString();
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
            var isSuccess = !state.HasError && state.IsDataLoaded;

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

            return isSuccess
                ? ApiResponse<GetMyQueueListResponse>.Success(GeneralCode.APP_MESSAGE_2001.ToString(), response)
                : ApiResponse<GetMyQueueListResponse>.Fail(state.ErrorCode ?? GeneralCode.APP_MESSAGE_5001.ToString());
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
            var isWaiting = status == QueueStatus.WAITING;
            var isCalling = status == QueueStatus.CALLING;
            var isCompleted = status == QueueStatus.COMPLETED;

            waiting += isWaiting ? 1 : 0;
            inProgress += isCalling ? 1 : 0;
            completed += isCompleted ? 1 : 0;
        }

        /// <summary>
        /// Maps a queue entity to its corresponding DTO for API response.
        /// Extracts related patient, service, and room information.
        /// </summary>
        /// <param name="queue">The queue entity to map.</param>
        /// <returns>A mapped DTO representing the queue item.</returns>
        private static MyQueueItemDto MapToQueueItem(Queue queue)
        {
            var appointment = queue.Appointment;
            var patient = appointment?.Patient;
            var service = appointment?.Service;
            var slot = appointment?.Slot;
            var room = queue.Room;

            var queueItem = new MyQueueItemDto
            {
                QueueId = queue.Id,
                QueueNumber = queue.QueueNumber,
                AppointmentId = queue.AppointmentId,
                PatientId = appointment?.PatientId ?? Guid.Empty,
                PatientName = DefaultString(patient?.FullName, "Unknown"),
                PatientPhone = patient?.PhoneNumber,
                PatientDateOfBirth = patient?.Dob,
                PatientGender = DefaultString(patient?.Gender.ToString(), "UNKNOWN"),
                AppointmentTime = CalculateAppointmentTime(appointment, slot),
                Symptoms = appointment?.Symptoms,
                RoomId = room?.Id,
                RoomName = room?.RoomName,
                Status = queue.Status,
                StatusText = GetStatusText(queue.Status),
                CalledAt = queue.CalledAt,
                CompletedAt = queue.CompletedAt,
                HasMedicalRecord = appointment?.MedicalRecord != null,
                HasPreliminaryDiagnosis = appointment?.PreliminaryDiagnosis != null,
                ServiceName = service?.ServiceName,
                BookingSource = DefaultString(appointment?.BookingSource, "UNKNOWN")
            };

            return queueItem;
        }

        /// <summary>
        /// Calculates the appointment time from the appointment date and time slot.
        /// Falls back to defaults if appointment or slot is null.
        /// </summary>
        /// <param name="appointment">The appointment entity containing the date.</param>
        /// <param name="slot">The time slot entity containing the start time.</param>
        /// <returns>The calculated appointment DateTime.</returns>
        private static DateTime CalculateAppointmentTime(Appointment? appointment, TimeSlot? slot)
        {
            var defaultTime = DateTime.UtcNow;
            var hasAppointment = appointment != null;
            var hasSlot = slot != null;

            var baseDate = hasAppointment ? appointment!.AppointmentDate.Date : defaultTime.Date;
            var timeOfDay = hasSlot ? slot!.StartTime.TimeOfDay : defaultTime.TimeOfDay;

            return baseDate.Add(timeOfDay);
        }

        /// <summary>
        /// Returns the specified default value if the input string is null or empty.
        /// </summary>
        /// <param name="value">The string value to check.</param>
        /// <param name="defaultValue">The default value to return if input is null or empty.</param>
        /// <returns>The original value or the default if null/empty.</returns>
        private static string DefaultString(string? value, string defaultValue)
        {
            return string.IsNullOrEmpty(value) ? defaultValue : value;
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
