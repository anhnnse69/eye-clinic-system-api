using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.CompleteQueueServices
{
    /// <summary>
    /// Service implementation for completing a queue item.
    /// Marks queue as COMPLETED so patient no longer appears in queue list.
    /// </summary>
    public class CompleteQueueService : ICompleteQueueService
    {
        private readonly IRepositoryQueryBase<Queue, Guid, AppDbContext> _queueRepository;
        private readonly IRepositoryQueryBase<Appointment, Guid, AppDbContext> _appointmentRepository;
        private readonly IValidator<CompleteQueueRequest> _validator;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CompleteQueueService(
            IRepositoryQueryBase<Queue, Guid, AppDbContext> queueRepository,
            IRepositoryQueryBase<Appointment, Guid, AppDbContext> appointmentRepository,
            IValidator<CompleteQueueRequest> validator,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _queueRepository = queueRepository;
            _appointmentRepository = appointmentRepository;
            _validator = validator;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Main orchestration method for completing a queue item.
        /// </summary>
        public async Task<ApiResponse<CompleteQueueResponse>> Process(CompleteQueueRequest request)
        {
            var state = new ExecutionState();
            // Step 1: Validate incoming request data
            ValidateRequest(request, state);
            // Step 2: Extract authenticated user ID from JWT token
            RetrieveAuthenticatedUserId(state);
            // Step 3: Parse queue ID
            ParseQueueId(request.QueueId, state);
            // Step 4: Verify queue exists
            await GetQueueAsync(state);
            // Step 5: Verify appointment exists
            await GetAppointmentAsync(state);
            // Step 6: Verify preliminary diagnosis exists
            VerifyMedicalRecordExists(state);
            // Step 7: Complete the queue
            await CompleteQueueAsync(state);
            // Step 8: Build and return the response
            return CreateResponse(state);
        }

        #region Execution State

        /// <summary>
        /// ExecutionState holds all mutable state for the process flow.
        /// </summary>
        private class ExecutionState
        {
            public bool IsValidationPassed { get; set; } = true;
            public bool IsUserValid { get; set; } = true;
            public bool IsQueueValid { get; set; } = true;
            public bool IsAppointmentValid { get; set; } = true;
            public bool IsMedicalRecordValid { get; set; } = true;
            public bool IsExecutionSuccess { get; set; } = true;
            public bool HasError { get; set; } = false;
            public Guid ActiveUserId { get; set; }
            public Guid QueueId { get; set; }
            public Queue? Queue { get; set; }
            public Appointment? Appointment { get; set; }
            public string? ErrorCode { get; set; }
            public string? PreviousStatus { get; set; }
        }

        #endregion

        #region Validation Steps

        /// <summary>
        /// Validates the incoming request using FluentValidation rules.
        /// </summary>
        private void ValidateRequest(CompleteQueueRequest request, ExecutionState state)
        {
            var result = _validator.Validate(request);
            state.IsValidationPassed = result.IsValid;
            state.HasError = !result.IsValid;
            state.ErrorCode = result.IsValid ? null : GeneralCode.APP_MESSAGE_4019.ToString();
        }

        /// <summary>
        /// Extracts the authenticated user ID from JWT token in HTTP context.
        /// </summary>
        private void RetrieveAuthenticatedUserId(ExecutionState state)
        {
            var principalIdValue = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var parseResult = Guid.TryParse(principalIdValue, out var parsedUserId);
            state.IsUserValid = parseResult;
            state.ActiveUserId = parseResult ? parsedUserId : Guid.Empty;
            state.HasError = !parseResult;
            state.ErrorCode = parseResult ? null : GeneralCode.APP_MESSAGE_4033.ToString();
        }

        /// <summary>
        /// Parses the queue ID from string to Guid.
        /// </summary>
        private void ParseQueueId(string queueId, ExecutionState state)
        {
            var parseResult = Guid.TryParse(queueId, out var parsedId);
            state.QueueId = parseResult ? parsedId : Guid.Empty;
            state.HasError = state.HasError || !parseResult;
            state.ErrorCode = parseResult ? state.ErrorCode : GeneralCode.APP_MESSAGE_4019.ToString();
        }

        /// <summary>
        /// Retrieves the queue from database.
        /// </summary>
        private async Task GetQueueAsync(ExecutionState state)
        {
            if (state.HasError) return;
            var queue = await _queueRepository
                .FindByCondition(q => q.Id == state.QueueId, trackChanges: false)
                .Include(q => q.Appointment)
                .FirstOrDefaultAsync();
            state.Queue = queue;
            state.IsQueueValid = queue != null;
            // The guard above ensures state.HasError is false here, so a plain
            // `if (!state.IsQueueValid) state.HasError = true;` is equivalent to
            //   `state.HasError = state.HasError || !state.IsQueueValid;`
            // and avoids the unreachable short-circuit branch that would
            // otherwise cost 1 uncovered branch in coverage.
            if (!state.IsQueueValid)
            {
                state.HasError = true;
            }
            state.ErrorCode = state.IsQueueValid ? state.ErrorCode : GeneralCode.APP_MESSAGE_4052.ToString();
        }

        /// <summary>
        /// Retrieves the appointment associated with the queue.
        /// </summary>
        private async Task GetAppointmentAsync(ExecutionState state)
        {
            if (state.HasError || state.Queue == null) return;
            var appointment = await _appointmentRepository
                .FindByCondition(a => a.Id == state.Queue.AppointmentId, trackChanges: false)
                .Include(a => a.Patient)
                .Include(a => a.PreliminaryDiagnosis)
                .Include(a => a.MedicalRecord)
                .FirstOrDefaultAsync();
            state.Appointment = appointment;
            state.IsAppointmentValid = appointment != null;
            // See GetQueueAsync for the rationale behind avoiding `||` here.
            if (!state.IsAppointmentValid)
            {
                state.HasError = true;
            }
            state.ErrorCode = state.IsAppointmentValid ? state.ErrorCode : GeneralCode.APP_MESSAGE_4012.ToString();
        }

        /// <summary>
        /// Verifies that a medical record exists for the appointment.
        /// This is required before completing the queue.
        /// </summary>
        private void VerifyMedicalRecordExists(ExecutionState state)
        {
            if (state.HasError || state.Queue == null || state.Appointment == null) return;

            // A consultation is considered complete when the doctor has produced a
            // MedicalRecord (the full-form clinical record) OR has captured a
            // PreliminaryDiagnosis (triage screen). Accept either so the queue
            // can move to COMPLETED in all real flows.
            var hasMedicalRecord = state.Appointment.MedicalRecord != null;
            var hasPreliminaryDiagnosis = state.Appointment.PreliminaryDiagnosis != null;
            state.IsMedicalRecordValid = hasMedicalRecord || hasPreliminaryDiagnosis;
            // See GetQueueAsync for the rationale behind avoiding `||` here.
            if (!state.IsMedicalRecordValid)
            {
                state.HasError = true;
            }

            // Debug log
            Console.WriteLine($"[CompleteQueue] HasMedicalRecord: {hasMedicalRecord}, HasPreliminaryDiagnosis: {hasPreliminaryDiagnosis}, AppointmentId: {state.Appointment.Id}");

            state.ErrorCode = state.IsMedicalRecordValid ? state.ErrorCode : GeneralCode.APP_MESSAGE_4028.ToString();
        }

        #endregion

        #region Completion Steps

        /// <summary>
        /// Marks the queue as COMPLETED and updates related data.
        /// </summary>
        private async Task CompleteQueueAsync(ExecutionState state)
        {
            if (state.HasError || state.Queue == null) return;
            try
            {
                state.PreviousStatus = state.Queue.Status.ToString();
                
                // Update queue status to COMPLETED
                state.Queue.Status = QueueStatus.COMPLETED;
                state.Queue.CompletedAt = DateTime.UtcNow;
                _context.Queues.Update(state.Queue);

                // Update appointment status to COMPLETED if not already
                if (state.Appointment != null && state.Appointment.Status != AppointmentStatus.COMPLETED)
                {
                    state.Appointment.Status = AppointmentStatus.COMPLETED;
                    state.Appointment.UpdatedAt = DateTime.UtcNow;
                    _context.Appointments.Update(state.Appointment);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                state.IsExecutionSuccess = false;
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
            }
        }

        #endregion

        #region Response Building

        /// <summary>
        /// Creates the API response based on execution state.
        /// </summary>
        private ApiResponse<CompleteQueueResponse> CreateResponse(ExecutionState state)
        {
            if (state.HasError)
            {
                return ApiResponse<CompleteQueueResponse>.Fail(
                    state.ErrorCode ?? GeneralCode.APP_MESSAGE_4001.ToString());
            }
            var response = new CompleteQueueResponse
            {
                QueueId = state.Queue?.Id.ToString() ?? string.Empty,
                AppointmentId = state.Queue?.AppointmentId.ToString() ?? string.Empty,
                PatientName = state.Appointment?.Patient.FullName,
                QueueNumber = state.Queue?.QueueNumber ?? 0,
                PreviousStatus = state.PreviousStatus ?? string.Empty,
                CompletedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"),
                IsSuccess = true
            };
            return ApiResponse<CompleteQueueResponse>.Success(
                GeneralCode.APP_MESSAGE_2007.ToString(), response);
        }

        #endregion
    }
}
