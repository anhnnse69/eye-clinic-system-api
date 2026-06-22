using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.PatientAppointmentManagementServices.CancelAppointmentServices
{
    /// <summary>
    /// Handles the business logic for cancelling appointments by patients.
    /// </summary>
    public class CancelAppointmentService : ICancelAppointmentService
    {
        private readonly IRepositoryBaseAsync<Appointment, Guid, AppDbContext> _appointmentRepository;
        private readonly IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> _slotRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="CancelAppointmentService"/> class with required infrastructure boundaries.
        /// </summary>
        /// <param name="appointmentRepository">Repository boundary instance for tracking persistent appointment changes.</param>
        /// <param name="slotRepository">Repository boundary instance for tracking persistent slot state adjustments.</param>
        /// <param name="httpContextAccessor">Accessor to safely retrieve authentication claims identities out of current HTTP request pipelines.</param>
        public CancelAppointmentService(
            IRepositoryBaseAsync<Appointment, Guid, AppDbContext> appointmentRepository,
            IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> slotRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _appointmentRepository = appointmentRepository;
            _slotRepository = slotRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes the internal business logic data pipeline to validate, cancel appointment, and release slot.
        /// </summary>
        /// <param name="request">The data container tracking cancellation parameters and structural entity keys.</param>
        /// <returns>An <see cref="ApiResponse{CancelAppointmentResponse}"/> enclosing descriptive state transaction payloads.</returns>
        public async Task<ApiResponse<CancelAppointmentResponse>> Process(CancelAppointmentRequest request)
        {
            // Initialize status tracking flags
            bool isUserValid = true;
            bool isAppointmentExist = true;
            bool isPermissionValid = true;
            bool isCancellationValid = true;

            // Step 1: Extract identity information metrics from active token pipelines
            var userId = RetrieveUserId(ref isUserValid);

            // Step 2: Retrieve the appointment with necessary related data
            var (appointment, appointmentExists) = await RetrieveAppointment(request.AppointmentId);
            isAppointmentExist = appointmentExists;

            // Step 3: Validate user permission and cancellation conditions
            (isPermissionValid, isCancellationValid) = ValidateCancellationPermissions(
                userId,
                appointment,
                isUserValid,
                isAppointmentExist);

            // Step 4: Process the cancellation (update appointment and release slot)
            var response = await ProcessCancellation(
                appointment!,
                request.Reason,
                isPermissionValid,
                isCancellationValid);

            // Step 5: Package contextual payloads dynamically to manage outcome states
            return CreateResponse(response, isUserValid, isPermissionValid, isCancellationValid, isAppointmentExist);
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
        /// Retrieves the appointment by ID with necessary navigation properties.
        /// </summary>
        /// <param name="appointmentId">The appointment ID to retrieve.</param>
        /// <returns>A tuple containing the appointment entity and a flag indicating existence.</returns>
        private async Task<(Appointment? Appointment, bool Exists)> RetrieveAppointment(Guid appointmentId)
        {
            // Step 1: Query database layers using optimized streams with eager loading
            var appointment = await _appointmentRepository
                .FindByCondition(a => a.Id == appointmentId, trackChanges: true)
                .Include(a => a.Slot)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync();

            if (appointment == null)
            {
                return (null, false);
            }

            return (appointment, true);
        }

        /// <summary>
        /// Validates whether the current user has permission to cancel the appointment.
        /// </summary>
        /// <param name="userId">The authenticated user ID.</param>
        /// <param name="appointment">The appointment entity.</param>
        /// <param name="isUserValid">Guard state monitoring authentication pipeline checkpoints.</param>
        /// <param name="isAppointmentExist">Guard state monitoring appointment existence.</param>
        /// <returns>A tuple containing permission validation flag and cancellation validation flag.</returns>
        private (bool IsPermissionValid, bool IsCancellationValid) ValidateCancellationPermissions(
            Guid userId,
            Appointment? appointment,
            bool isUserValid,
            bool isAppointmentExist)
        {
            // Step 1: Validate authentication
            if (!isUserValid)
            {
                return (false, false);
            }

            // Step 2: Validate appointment existence
            if (!isAppointmentExist || appointment == null)
            {
                return (false, false);
            }

            // Step 3: Check if the appointment belongs to the patient
            if (appointment.PatientId != userId && appointment.CreatedById != userId)
            {
                return (false, false);
            }

            // Step 4: Check if appointment is already cancelled
            if (appointment.Status == AppointmentStatus.CANCELLED)
            {
                return (true, false);
            }

            // Step 5: Check if appointment is already completed
            if (appointment.Status == AppointmentStatus.COMPLETED)
            {
                return (true, false);
            }

            // Step 6: Check if appointment is in progress
            if (appointment.Status == AppointmentStatus.IN_PROGRESS)
            {
                return (true, false);
            }

            // Step 7: Check cancellation deadline (24 hours before appointment)
            var hoursUntilAppointment = (appointment.AppointmentDate - DateTime.Now).TotalHours;
            if (hoursUntilAppointment < 24)
            {
                return (true, false);
            }

            // Step 8: Check if appointment is in a cancellable status
            if (appointment.Status != AppointmentStatus.PENDING)
            {
                return (true, false);
            }

            return (true, true);
        }

        /// <summary>
        /// Processes the cancellation by updating appointment status and releasing the slot.
        /// </summary>
        /// <param name="appointment">The appointment entity.</param>
        /// <param name="reason">The cancellation reason.</param>
        /// <param name="isPermissionValid">Guard state flag tracking permission validation.</param>
        /// <param name="isCancellationValid">Guard state flag tracking cancellation conditions.</param>
        /// <returns>A response containing cancellation details.</returns>
        private async Task<CancelAppointmentResponse?> ProcessCancellation(
            Appointment appointment,
            string? reason,
            bool isPermissionValid,
            bool isCancellationValid)
        {
            if (!isPermissionValid || !isCancellationValid || appointment == null)
            {
                return null;
            }

            using var transaction = await _appointmentRepository.BeginTransactionAsync();

            try
            {
                // Step 1: Update appointment status
                appointment.Status = AppointmentStatus.CANCELLED;
                appointment.NoteReason = reason ?? "Bệnh nhân hủy lịch hẹn";
                appointment.UpdatedAt = DateTime.Now;

                await _appointmentRepository.UpdateAsync(appointment);

                // Step 2: Release the slot
                await ReleaseSlot(appointment.SlotId);

                // Step 3: Commit transaction
                await transaction.CommitAsync();

                // Step 4: Build response
                var response = new CancelAppointmentResponse
                {
                    AppointmentId = appointment.Id,
                    Status = appointment.Status.ToString(),
                    CancelledAt = appointment.UpdatedAt,
                    CancellationReason = appointment.NoteReason,
                    Message = "Hủy lịch hẹn thành công"
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
        /// Releases the slot by decrementing current patients and updating slot status.
        /// </summary>
        /// <param name="slotId">The slot ID to release.</param>
        private async Task ReleaseSlot(Guid slotId)
        {
            // Step 1: Retrieve the slot with tracking
            var slot = await _slotRepository
                .FindByCondition(s => s.Id == slotId, trackChanges: true)
                .FirstOrDefaultAsync();

            if (slot != null)
            {
                // Step 2: Decrement current patients
                slot.CurrentPatients = Math.Max(0, slot.CurrentPatients - 1);

                // Step 3: Update slot status if not full anymore
                if (slot.CurrentPatients < slot.MaxPatients)
                {
                    slot.Status = SlotStatus.AVAILABLE;
                }

                // Step 4: Save changes
                await _slotRepository.UpdateAsync(slot);
            }
        }

        /// <summary>
        /// Resolves transaction outcome wrappers packing serialization nodes safely.
        /// </summary>
        /// <param name="response">The internal serializable structure returned out of core projection chains.</param>
        /// <param name="isUserValid">Guard context parameter evaluating token claim validity bounds.</param>
        /// <param name="isPermissionValid">Guard monitoring parameter checking permission boundaries.</param>
        /// <param name="isCancellationValid">Guard monitoring parameter checking cancellation conditions.</param>
        /// <param name="isAppointmentExist">Guard monitoring parameter checking appointment existence.</param>
        /// <returns>A structured envelope holding operational response outcomes ready for presentation nodes.</returns>
        private ApiResponse<CancelAppointmentResponse> CreateResponse(
            CancelAppointmentResponse? response,
            bool isUserValid,
            bool isPermissionValid,
            bool isCancellationValid,
            bool isAppointmentExist)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isPermissionValid, isCancellationValid, isAppointmentExist);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<CancelAppointmentResponse>.Success(
                GeneralCode.APP_MESSAGE_2004.ToString(), // Appointment cancelled successfully
                response!);
        }

        /// <summary>
        /// Evaluates functional exceptions sequences to render failure metadata nodes.
        /// </summary>
        /// <param name="isUserValid">Guard indicating whether authorization checkpoints cleared successfully.</param>
        /// <param name="isPermissionValid">Guard indicating whether permission validation cleared successfully.</param>
        /// <param name="isCancellationValid">Guard indicating whether cancellation conditions are met.</param>
        /// <param name="isAppointmentExist">Guard indicating whether appointment exists.</param>
        /// <returns>A failure configuration block, or null if execution tracks meet standard benchmarks.</returns>
        private ApiResponse<CancelAppointmentResponse>? CreateErrorResponse(
            bool isUserValid,
            bool isPermissionValid,
            bool isCancellationValid,
            bool isAppointmentExist)
        {
            if (!isUserValid)
            {
                return ApiResponse<CancelAppointmentResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4001.ToString());
            }

            if (!isAppointmentExist)
            {
                return ApiResponse<CancelAppointmentResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4046.ToString()); // Appointment not found
            }

            if (!isPermissionValid)
            {
                return ApiResponse<CancelAppointmentResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4053.ToString()); // Patient does not have permission to cancel this appointment
            }

            if (!isCancellationValid)
            {
                return ApiResponse<CancelAppointmentResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4050.ToString()); // Cannot cancel appointment within 24 hours
            }

            return null;
        }
    }
}