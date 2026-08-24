using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.GetQueueListByIdServices
{
    /// <summary>
    /// Service for retrieving the queue list for a specific doctor by their profile ID.
    /// Used by admins and receptionists to view any doctor's queue.
    /// </summary>
    public class GetQueueListByIdService : IGetQueueListByIdService
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Initializes a new instance of the service with the database context.
        /// </summary>
        /// <param name="context">The application database context for querying queue and doctor profile data.</param>
        public GetQueueListByIdService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Processes the request to retrieve the queue list for a specific doctor on the specified date.
        /// </summary>
        /// <param name="doctorId">The doctor profile ID to retrieve queue for.</param>
        /// <param name="date">The date for which to retrieve the queue list.</param>
        /// <returns>An API response containing the queue list data or an error code on failure.</returns>
        public async Task<ApiResponse<GetQueueListByIdResponse>> Process(Guid doctorId, DateOnly date)
        {
            var state = new ExecutionState { DoctorId = doctorId };
            await ValidateDoctorAsync(doctorId, state);

            if (state.HasError)
            {
                return ApiResponse<GetQueueListByIdResponse>.Fail(state.ErrorCode!);
            }

            await GetQueueDataAsync(doctorId, date, state);
            return CreateResponse(state, date);
        }

        /// <summary>
        /// Holds all mutable execution state for the process flow.
        /// Tracks doctor validation, data loading, and error status.
        /// </summary>
        private class ExecutionState
        {
            /// <summary>The doctor profile ID to retrieve queue for.</summary>
            public Guid DoctorId { get; set; } = Guid.Empty;

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
        /// Validates that the specified doctor exists and is active.
        /// </summary>
        /// <param name="doctorId">The doctor profile ID to validate.</param>
        /// <param name="state">The execution state to update with validation result.</param>
        private async Task ValidateDoctorAsync(Guid doctorId, ExecutionState state)
        {
            var doctor = await _context.Set<DoctorProfile>()
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == doctorId && d.IsActive);

            var isDoctorExists = doctor != null;
            state.IsDoctorExists = isDoctorExists;
            state.HasError = !isDoctorExists;
            state.ErrorCode = isDoctorExists ? null : GeneralCode.APP_MESSAGE_4011.ToString();
        }

        /// <summary>
        /// Retrieves the queue data for the specified doctor and date from the database.
        /// Eager loads related appointment, patient, service, slot, medical record, and room entities.
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
                    .ThenInclude(a => a!.Slot)
                .Include(q => q.Appointment)
                    .ThenInclude(a => a!.MedicalRecord)
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
        private ApiResponse<GetQueueListByIdResponse> CreateResponse(ExecutionState state, DateOnly date)
        {
            var waitingCount = 0;
            var inProgressCount = 0;
            var completedCount = 0;
            var queueItems = new List<QueueByIdItemDto>();

            foreach (var queue in state.Queues)
            {
                var item = MapToQueueItem(queue);
                queueItems.Add(item);
                CountByStatus(queue.Status, ref waitingCount, ref inProgressCount, ref completedCount);
            }

            var response = new GetQueueListByIdResponse
            {
                Date = date,
                DoctorId = state.DoctorId,
                TotalPatients = state.Queues.Count,
                WaitingCount = waitingCount,
                InProgressCount = inProgressCount,
                CompletedCount = completedCount,
                Items = queueItems
            };

            return ApiResponse<GetQueueListByIdResponse>.Success(GeneralCode.APP_MESSAGE_2001.ToString(), response);
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
            // CALLING = the doctor just called the patient to the exam-room door;
            // IN_PROGRESS = the medical record has been saved but summary +
            // prescription (Step 5+6) are still pending. Both count as
            // "Examination in progress" in the dashboard.
            var isWaiting = status == QueueStatus.WAITING;
            var isInProgress = status == QueueStatus.CALLING || status == QueueStatus.IN_PROGRESS;
            var isCompleted = status == QueueStatus.COMPLETED;

            waiting += isWaiting ? 1 : 0;
            inProgress += isInProgress ? 1 : 0;
            completed += isCompleted ? 1 : 0;
        }

        /// <summary>
        /// Maps a queue entity to its corresponding DTO for API response.
        /// Extracts related patient, service, and room information.
        /// </summary>
        /// <param name="queue">The queue entity to map.</param>
        /// <returns>A mapped DTO representing the queue item.</returns>
        private static QueueByIdItemDto MapToQueueItem(Queue queue)
        {
            var appointment = queue.Appointment;
            var patient = appointment.Patient;
            var service = appointment.Service;
            var slot = appointment.Slot;
            var room = queue.Room;

            Guid? roomId = null;
            string? roomName = null;
            if (room != null)
            {
                roomId = room.Id;
                roomName = room.RoomName;
            }

            var serviceName = service != null ? service.ServiceName : null;

            var queueItem = new QueueByIdItemDto
            {
                QueueId = queue.Id,
                QueueNumber = queue.QueueNumber,
                AppointmentId = appointment.Id,
                PatientId = appointment.PatientId,
                PatientName = DefaultString(patient.FullName, "Unknown"),
                PatientPhone = patient.PhoneNumber,
                PatientDateOfBirth = patient.Dob,
                PatientGender = DefaultString(patient.Gender.ToString(), "UNKNOWN"),
                AppointmentTime = CalculateAppointmentTime(appointment, slot),
                Symptoms = appointment.Symptoms,
                RoomId = roomId,
                RoomName = roomName,
                Status = queue.Status,
                StatusText = GetStatusText(queue.Status),
                CalledAt = queue.CalledAt,
                CompletedAt = queue.CompletedAt,
                HasMedicalRecord = appointment.MedicalRecord != null,
                ServiceName = serviceName,
                BookingSource = DefaultString(appointment.BookingSource, "UNKNOWN")
            };

            return queueItem;
        }

        /// <summary>
        /// Calculates the appointment time from the appointment date and time slot.
        /// </summary>
        /// <param name="appointment">The appointment entity containing the date.</param>
        /// <param name="slot">The time slot entity containing the start time.</param>
        /// <returns>The calculated appointment DateTime.</returns>
        private static DateTime CalculateAppointmentTime(Appointment appointment, TimeSlot slot)
        {
            return appointment.AppointmentDate.Date.Add(slot.StartTime.TimeOfDay);
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
                QueueStatus.IN_PROGRESS => "Đang khám",
                QueueStatus.COMPLETED => "Đã khám xong",
                _ => status.ToString()
            };
        }
    }
}
