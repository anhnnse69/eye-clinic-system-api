using ECS.Application.Common.Response;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistCancelAppointmentsServices
{
    /// <summary>
    /// Handles the business logic and transaction safety boundaries for cancelling system appointment graphs.
    /// </summary>
    public class ReceptionistCancelAppointmentsService : IReceptionistCancelAppointmentsService
    {
        private readonly IRepositoryBaseAsync<Appointment, Guid, AppDbContext> _appointmentRepo;
        private readonly IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> _timeSlotRepo;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistCancelAppointmentsService"/> with required state mutation repositories.
        /// </summary>
        /// <param name="appointmentRepo">The mutation repository tracking medical appointments.</param>
        /// <param name="timeSlotRepo">The mutation repository tracking shift scheduler resource segments.</param>
        public ReceptionistCancelAppointmentsService(
            IRepositoryBaseAsync<Appointment, Guid, AppDbContext> appointmentRepo,
            IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> timeSlotRepo)
        {
            _appointmentRepo = appointmentRepo;
            _timeSlotRepo = timeSlotRepo;
        }

        /// <summary>
        /// Core orchestration unit delegating execution flows into an isolated transaction management context.
        /// </summary>
        /// <param name="request">The filtration and modification data structure payload context.</param>
        /// <returns>A structured successful or exceptional validation <see cref="ApiResponse{ReceptionistCancelAppointmentsResponse}"/> wrapper.</returns>
        public async Task<ApiResponse<ReceptionistCancelAppointmentsResponse>> Process(ReceptionistCancelAppointmentsRequest request)
        {
            return await ExecuteCancelTransactionPipeline(request.AppointmentId, request.NoteReason);
        }

        /// <summary>
        /// Runs the transactional pipeline to update appointment status profiles and adjust matching reservation capacities.
        /// </summary>
        /// <param name="appointmentId">The target database primary identifier for the appointment graph.</param>
        /// <param name="reason">The explanation justification notes required for compliance records.</param>
        /// <returns>The outcome envelope containing transaction finalization markers.</returns>
        private async Task<ApiResponse<ReceptionistCancelAppointmentsResponse>> ExecuteCancelTransactionPipeline(Guid appointmentId, string reason)
        {
            using var transaction = await _appointmentRepo.BeginTransactionAsync();
            try
            {
                var appointment = await _appointmentRepo.FindByCondition(
                    ap => ap.Id == appointmentId,
                    trackChanges: true
                ).Include(ap => ap.Slot)
                 .FirstOrDefaultAsync();
                var validationError = ValidateCancelCriteria(appointment, reason);
                if (validationError != null) return validationError;
                // Step 1: Update the appointment state markers to CANCELLED and log the compliance justification notes
                appointment!.Status = AppointmentStatus.CANCELLED;
                appointment.NoteReason = reason;
                appointment.UpdatedAt = DateTime.UtcNow;
                await _appointmentRepo.UpdateAsync(appointment);
                // Step 2: Release the associated scheduler time slot allocation back to the clinic repository pool
                var slot = appointment.Slot;
                slot.CurrentPatients = Math.Max(0, slot.CurrentPatients - 1);
                // Open the availability status back to AVAILABLE if the segment was previously fully BOOKED
                if (slot.Status == SlotStatus.BOOKED && slot.CurrentPatients < slot.MaxPatients)
                {
                    slot.Status = SlotStatus.AVAILABLE;
                }
                await _timeSlotRepo.UpdateAsync(slot);
                await _appointmentRepo.EndTransactionAsync();
                // Step 3: Map operational changes and build success data presentation models
                return AssembleSuccessCancelResponse(appointment);
            }
            catch (Exception)
            {
                await _appointmentRepo.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Audits business validation rulesets targeting specific active state models and configuration parameters.
        /// </summary>
        /// <param name="appointment">The targeting appointment configuration entity tracking values.</param>
        /// <param name="reason">The required textual cancellation code reason argument.</param>
        /// <returns>A failure description wrapper if requirements fail; otherwise null.</returns>
        private ApiResponse<ReceptionistCancelAppointmentsResponse>? ValidateCancelCriteria(Appointment? appointment, string reason)
        {
            if (appointment == null)
                return ApiResponse<ReceptionistCancelAppointmentsResponse>.Fail("APPOINTMENT_NOT_FOUND");
            if (string.IsNullOrWhiteSpace(reason))
                return ApiResponse<ReceptionistCancelAppointmentsResponse>.Fail("CANCELLATION_REASON_REQUIRED");
            if (appointment.Status == AppointmentStatus.CANCELLED)
                return ApiResponse<ReceptionistCancelAppointmentsResponse>.Fail("APPOINTMENT_ALREADY_CANCELLED");
            if (appointment.Status == AppointmentStatus.COMPLETED ||
                appointment.Status == AppointmentStatus.IN_PROGRESS ||
                appointment.Status == AppointmentStatus.ARRIVED)
            {
                return ApiResponse<ReceptionistCancelAppointmentsResponse>.Fail("INVALID_STATUS_FOR_CANCELLATION");
            }
            return null;
        }

        /// <summary>
        /// Wraps the updated tracking graph information structures inside normalized framework response envelopes.
        /// </summary>
        /// <param name="appointment">The mutated entity model values source framework reference.</param>
        /// <returns>A structured successful operational response payload context.</returns>
        private ApiResponse<ReceptionistCancelAppointmentsResponse> AssembleSuccessCancelResponse(Appointment appointment)
        {
            return ApiResponse<ReceptionistCancelAppointmentsResponse>.Success("APP_MESSAGE_2000", new ReceptionistCancelAppointmentsResponse
            {
                AppointmentId = appointment.Id.ToString(),
                Status = appointment.Status.ToString().ToUpper(),
                UpdatedAt = appointment.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            });
        }
    }
}