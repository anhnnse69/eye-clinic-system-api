using ECS.Application.Common.Response;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ConfirmRejectAppointmentsServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ReceptionistAppointmentManagementServices.ConfirmRejectAppointmentsServices
{
    /// <summary>
    /// Handles confirming or rejecting appointments on behalf of the
    /// authenticated receptionist. The receptionist's clinic is resolved
    /// via their active <see cref="StaffClinic"/> assignment, and the
    /// target appointment must belong to a doctor within that same clinic
    /// (the receptionist is not restricted to a single doctor).
    /// </summary>
    public class ReceptionistConfirmRejectAppointmentService : IReceptionistConfirmRejectAppointmentService
    {
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepo;
        private readonly IRepositoryBaseAsync<Appointment, Guid, AppDbContext> _appointmentCommandRepo;
        private readonly IRepositoryQueryBase<Appointment, Guid, AppDbContext> _appointmentQueryRepo;
        private readonly IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> _slotCommandRepo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceptionistConfirmRejectAppointmentService"/>.
        /// </summary>
        public ReceptionistConfirmRejectAppointmentService(
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepo,
            IRepositoryBaseAsync<Appointment, Guid, AppDbContext> appointmentCommandRepo,
            IRepositoryQueryBase<Appointment, Guid, AppDbContext> appointmentQueryRepo,
            IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> slotCommandRepo,
            AppDbContext dbContext)
        {
            _staffClinicRepo = staffClinicRepo;
            _appointmentCommandRepo = appointmentCommandRepo;
            _appointmentQueryRepo = appointmentQueryRepo;
            _slotCommandRepo = slotCommandRepo;
        }

        /// <summary>
        /// Confirms or rejects an appointment belonging to any doctor
        /// within the receptionist's clinic.
        /// </summary>
        /// <param name="receptionistUserId">Identifier of the authenticated receptionist's user account.</param>
        /// <param name="appointmentId">Identifier of the appointment.</param>
        /// <param name="request">Decision request.</param>
        /// <returns>Updated appointment information.</returns>
        public async Task<ApiResponse<ConfirmRejectAppointmentResponse>> Process(
            Guid receptionistUserId,
            Guid appointmentId,
            ConfirmRejectAppointmentRequest request)
        {
            var clinicId = await ResolveReceptionistClinicIdAsync(receptionistUserId);
            var appointment = await ResolveAppointmentAsync(appointmentId, clinicId);
            await ApplyDecisionAsync(appointment, request);
            await PersistAppointmentAsync(appointment);
            var response = BuildResponse(appointment);
            return CreateSuccessResponse(response);
        }

        /// <summary>
        /// Resolves the clinic that the receptionist (current user) belongs to,
        /// via their active <see cref="StaffClinic"/> assignment.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when the receptionist has no active clinic assignment.</exception>
        private async Task<Guid> ResolveReceptionistClinicIdAsync(Guid receptionistUserId)
        {
            var staffClinic = await _staffClinicRepo
                .FindByCondition(sc => sc.UserId == receptionistUserId && sc.IsActive)
                .FirstOrDefaultAsync();

            if (staffClinic is null)
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4008.ToString());

            return staffClinic.ClinicId;
        }

        /// <summary>
        /// Retrieves the appointment, ensuring its doctor belongs to the
        /// receptionist's clinic. Throws when not found or out of scope.
        /// </summary>
        private async Task<Appointment> ResolveAppointmentAsync(Guid appointmentId, Guid clinicId)
        {
            var appointment = await _appointmentQueryRepo
                .FindByCondition(a => a.Id == appointmentId && a.Doctor.ClinicId == clinicId)
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
        /// Applies the receptionist's decision to the appointment.
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