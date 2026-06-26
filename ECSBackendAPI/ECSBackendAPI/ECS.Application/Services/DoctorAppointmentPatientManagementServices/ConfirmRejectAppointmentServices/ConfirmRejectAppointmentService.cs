using ECS.Application.Common.Response;
using ECS.Application.Services.PatientAppointmentManagementServices.ViewNotificationListServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ConfirmRejectAppointmentServices
{
    /// <summary>
    /// Handles confirming or rejecting appointments for the authenticated doctor.
    /// </summary>
    public class ConfirmRejectAppointmentService : IConfirmRejectAppointmentService
    {
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepo;
        private readonly IRepositoryBaseAsync<Appointment, Guid, AppDbContext> _appointmentCommandRepo;
        private readonly IRepositoryQueryBase<Appointment, Guid, AppDbContext> _appointmentQueryRepo;
        private readonly IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> _slotCommandRepo;
        private readonly INotificationCreationService _notificationService;
        AppDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmRejectAppointmentService"/>.
        /// </summary>
        public ConfirmRejectAppointmentService(
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepo,
            IRepositoryBaseAsync<Appointment, Guid, AppDbContext> appointmentCommandRepo,
            IRepositoryQueryBase<Appointment, Guid, AppDbContext> appointmentQueryRepo,
            IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> slotCommandRepo,
            INotificationCreationService notificationService,
            AppDbContext dbContext)
        {
            _doctorRepo = doctorRepo;
            _appointmentCommandRepo = appointmentCommandRepo;
            _appointmentQueryRepo = appointmentQueryRepo;
            _slotCommandRepo = slotCommandRepo;
            _notificationService = notificationService;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Confirms or rejects an appointment.
        /// </summary>
        /// <param name="userId">Identifier of the authenticated user.</param>
        /// <param name="appointmentId">Identifier of the appointment.</param>
        /// <param name="request">Decision request.</param>
        /// <returns>Updated appointment information.</returns>
        public async Task<ApiResponse<ConfirmRejectAppointmentResponse>> Process(
            Guid userId,
            Guid appointmentId,
            ConfirmRejectAppointmentRequest request)
        {
            var doctor = await ResolveDoctorAsync(userId);
            var appointment = await ResolveAppointmentAsync(appointmentId, doctor.Id);
            await ApplyDecisionAsync(appointment, request);
            await PersistAppointmentAsync(appointment);
            await NotifyPatientAsync(appointment, doctor, request.Decision);
            var response = BuildResponse(appointment);
            return CreateSuccessResponse(response);
        }

        /// <summary>
        /// Resolves the active doctor profile from the authenticated user.
        /// </summary>
        private async Task<DoctorProfile> ResolveDoctorAsync(Guid userId)
        {
            var doctor = await _doctorRepo
                .FindByCondition(d => d.UserId == userId && d.IsActive)
                .FirstOrDefaultAsync();
            if (doctor is null)
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4008.ToString());
            return doctor;
        }

        /// <summary>
        /// Retrieves the appointment belonging to the specified doctor.
        /// </summary>
        private async Task<Appointment> ResolveAppointmentAsync(Guid appointmentId, Guid doctorId)
        {
            var appointment = await _appointmentQueryRepo
                .FindByCondition(a => a.Id == appointmentId && a.DoctorId == doctorId)
                .Include(a => a.Slot)
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
                .FirstOrDefaultAsync();
            if (appointment is null)
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4004.ToString());
            return appointment;
        }

        /// <summary>
        /// Applies the doctor's decision to the appointment.
        /// </summary>
        private async Task ApplyDecisionAsync(Appointment appointment, ConfirmRejectAppointmentRequest request)
        {
            switch (request.Decision)
            {
                case AppointmentDecision.CONFIRM:
                    ApplyConfirmDecision(appointment, request.RejectReason);
                    break;
                case AppointmentDecision.REJECT:
                    await ApplyRejectDecisionAsync(appointment, request.RejectReason);
                    break;
            }
            appointment.UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Applies confirmation logic.
        /// </summary>
        private static void ApplyConfirmDecision(Appointment appointment, string? noteReason)
        {
            appointment.Status = AppointmentStatus.BOOKED;
            if (!string.IsNullOrWhiteSpace(noteReason))
                appointment.NoteReason = noteReason.Trim();
        }

        /// <summary>
        /// Applies rejection logic and releases the occupied slot.
        /// </summary>
        private async Task ApplyRejectDecisionAsync(Appointment appointment, string? noteReason)
        {
            appointment.Status = AppointmentStatus.CANCELLED;
            if (!string.IsNullOrWhiteSpace(noteReason))
                appointment.NoteReason = noteReason.Trim();
            await ReleaseSlotAsync(appointment.Slot);
        }

        /// <summary>
        /// Releases a slot when an appointment is rejected.
        /// </summary>
        private async Task ReleaseSlotAsync(TimeSlot? slot)
        {
            if (slot is null || slot.CurrentPatients <= 0)
                return;
            slot.CurrentPatients--;
            if (slot.Status == SlotStatus.BOOKED && slot.CurrentPatients < slot.MaxPatients)
                slot.Status = SlotStatus.AVAILABLE;
            await _slotCommandRepo.UpdateAsync(slot);
        }

        /// <summary>
        /// Persists appointment changes to database.
        /// </summary>
        private async Task PersistAppointmentAsync(Appointment appointment)
        {
            await _appointmentCommandRepo.UpdateAsync(appointment);
            await _appointmentCommandRepo.SaveChangesAsync();
        }

        /// <summary>
        /// Sends a notification to the patient about the doctor's decision.
        /// Silently skipped if the patient has no linked user account
        /// (e.g. created by a receptionist without registering an account).
        /// </summary>
        private async Task NotifyPatientAsync(
            Appointment appointment,
            DoctorProfile doctor,
            AppointmentDecision decision)
        {
            var patientUserId = appointment.Patient?.User?.Id;
            if (patientUserId is null || patientUserId == Guid.Empty)
            {
                patientUserId = await _dbContext.Set<UserPatient>()
                    .Where(up => up.PatientId == appointment.PatientId)
                    .Select(up => (Guid?)up.UserId)
                    .FirstOrDefaultAsync();
            }
            if (patientUserId is null || patientUserId == Guid.Empty)
                return;
            string templateKey = decision == AppointmentDecision.CONFIRM
                ? "APPOINTMENT_CONFIRMED"
                : "APPOINTMENT_REJECTED";
            var payload = new Dictionary<string, string>
             {
                     { "DoctorName", doctor.User?.FullName ?? "" },
                     { "PatientName", appointment.Patient?.FullName ?? "" },
                     { "AppointmentTime", appointment.AppointmentDate.ToString("yyyy-MM-ddTHH:mm:ssZ") },
                     { "RejectReason", appointment.NoteReason ?? string.Empty }
             };
            string payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);
            await _notificationService.Process(patientUserId.Value, templateKey, payloadJson);
        }

        /// <summary>
        /// Builds the response model.
        /// </summary>
        private static ConfirmRejectAppointmentResponse BuildResponse(Appointment appointment)
        {
            return new ConfirmRejectAppointmentResponse
            {
                AppointmentId = appointment.Id,
                Status = appointment.Status.ToString(),
                NoteReason = appointment.NoteReason,
                UpdatedAt = appointment.UpdatedAt,
            };
        }

        /// <summary>
        /// Creates a successful API response.
        /// </summary>
        private static ApiResponse<ConfirmRejectAppointmentResponse> CreateSuccessResponse(
            ConfirmRejectAppointmentResponse response)
        {
            return ApiResponse<ConfirmRejectAppointmentResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }
    }
}