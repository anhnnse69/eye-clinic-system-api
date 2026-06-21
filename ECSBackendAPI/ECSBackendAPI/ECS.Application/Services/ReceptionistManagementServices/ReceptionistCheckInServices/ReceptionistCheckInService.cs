using ECS.Application.Common.Response;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistCheckInServices
{
    /// <summary>
    /// Handles the business logic for verifying arrival eligibility bounds, computing sequential timeline indices, and executing atomicity blocks with room isolation.
    /// </summary>
    public class ReceptionistCheckInService : IReceptionistCheckInService
    {
        private readonly IRepositoryBaseAsync<Appointment, Guid, AppDbContext> _appointmentRepo;
        private readonly IRepositoryBaseAsync<Queue, Guid, AppDbContext> _queueRepo;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistCheckInService"/> with isolated data write-access gateways.
        /// </summary>
        /// <param name="appointmentRepo">The read-write repository interface for managing appointment entity aggregates.</param>
        /// <param name="queueRepo">The read-write repository interface for managing sequence configuration queue lines.</param>
        public ReceptionistCheckInService(
            IRepositoryBaseAsync<Appointment, Guid, AppDbContext> appointmentRepo,
            IRepositoryBaseAsync<Queue, Guid, AppDbContext> queueRepo)
        {
            _appointmentRepo = appointmentRepo;
            _queueRepo = queueRepo;
        }

        /// <summary>
        /// Acts as the core execution orchestrator regulating the structural pipeline flow and delegating branch logic down to an encapsulated transaction framework without inline control statements.
        /// </summary>
        /// <param name="request">The filtration parameters containing client-side reservation arguments.</param>
        /// <returns>A structured <see cref="ApiResponse{T}"/> packing matched arrival presentation data matrices.</returns>
        public async Task<ApiResponse<ReceptionistCheckInResponse>> Process(ReceptionistCheckInRequest request)
        {
            return await ExecuteCheckInTransactionPipeline(request.AppointmentId);
        }

        /// <summary>
        /// Executes the data update process within an isolated database transaction block to guarantee sequence number progression atomicity.
        /// </summary>
        /// <param name="appointmentId">The structural core validation token reference pointing to the unique appointment record.</param>
        /// <returns>A uniform success response enclosing mutated appointment states and structural queue trackers, or short-circuit error codes.</returns>
        private async Task<ApiResponse<ReceptionistCheckInResponse>> ExecuteCheckInTransactionPipeline(Guid appointmentId)
        {
            using var transaction = await _appointmentRepo.BeginTransactionAsync();
            try
            {
                var appointment = await _appointmentRepo.FindByCondition(
                    ap => ap.Id == appointmentId,
                    trackChanges: true,
                    ap => ap.Slot,
                    ap => ap.Slot.Schedule,
                    ap => ap.Doctor
                ).Include(ap => ap.Slot.Schedule.Room)
                 .FirstOrDefaultAsync();
                var validationError = ValidateCheckInCriteria(appointment);
                if (validationError != null) return validationError;
                Guid targetRoomId = appointment!.Slot.Schedule.Room?.Id ?? Guid.Empty;
                DateTime todayVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Date;
                DateTime startOfDayUtc = TimeZoneInfo.ConvertTimeToUtc(todayVn, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
                DateTime endOfDayUtc = startOfDayUtc.AddDays(1);
                int maxQueueNumber = await _queueRepo.FindByCondition(
                    q => q.RoomId == targetRoomId && q.CreatedAt >= startOfDayUtc && q.CreatedAt < endOfDayUtc,
                    trackChanges: false
                ).Select(q => (int?)q.QueueNumber).MaxAsync() ?? 0;
                int nextNumber = maxQueueNumber + 1;
                appointment.Status = AppointmentStatus.ARRIVED;
                appointment.UpdatedAt = DateTime.UtcNow;
                await _appointmentRepo.UpdateAsync(appointment);
                var newQueue = new Queue
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointment.Id,
                    ClinicId = appointment.Doctor.ClinicId,
                    RoomId = targetRoomId,
                    QueueNumber = nextNumber,
                    Status = QueueStatus.WAITING,
                    CreatedAt = DateTime.UtcNow
                };
                await _queueRepo.CreateAsync(newQueue);
                await _appointmentRepo.EndTransactionAsync();
                return AssembleSuccessCheckInResponse(appointment, newQueue);
            }
            catch (Exception)
            {
                await _appointmentRepo.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Evaluates arrival compliance constraints including target calendar dates, processing statuses, financial safety blocks, and room availability maps.
        /// </summary>
        /// <param name="appointment">The target aggregate root database graph instance to evaluate.</param>
        /// <returns>An optional <see cref="ApiResponse{T}"/> wrapping broken constraints, or null if all pipeline parameters clear successfully.</returns>
        private ApiResponse<ReceptionistCheckInResponse>? ValidateCheckInCriteria(Appointment? appointment)
        {
            if (appointment == null)
                return ApiResponse<ReceptionistCheckInResponse>.Fail("APPOINTMENT_NOT_FOUND");
            DateTime todayVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Date;
            if (appointment.AppointmentDate.Date != todayVn)
                return ApiResponse<ReceptionistCheckInResponse>.Fail("ERROR_NOT_TODAY");
            if (appointment.Status != AppointmentStatus.CONFIRMED &&
        appointment.Status != AppointmentStatus.BOOKED &&
        appointment.Status != AppointmentStatus.NOSHOW)
            {
                return ApiResponse<ReceptionistCheckInResponse>.Fail("INVALID_STATUS_FOR_CHECKIN");
            }
            if (!appointment.DepositPaid)
                return ApiResponse<ReceptionistCheckInResponse>.Fail("DEPOSIT_MUST_BE_PAID_FIRST");
            if (appointment.Slot?.Schedule?.Room == null)
                return ApiResponse<ReceptionistCheckInResponse>.Fail("DOCTOR_ROOM_NOT_CONFIGURED");
            return null;
        }

        /// <summary>
        /// Assembles separate mutated transactional elements into a standardized system response tracking structure.
        /// </summary>
        /// <param name="appointment">The finalized target appointment tracking data row.</param>
        /// <param name="queue">The freshly instantiated real-time sequential queue structure tracking item.</param>
        /// <returns>A uniform success api packet containing updated data arrays and presentation elements.</returns>
        private ApiResponse<ReceptionistCheckInResponse> AssembleSuccessCheckInResponse(Appointment appointment, Queue queue)
        {
            return ApiResponse<ReceptionistCheckInResponse>.Success("APP_MESSAGE_2000", new ReceptionistCheckInResponse
            {
                AppointmentId = appointment.Id.ToString(),
                Status = appointment.Status.ToString().ToUpper(),
                Queue = new QueueInlineRowDto
                {
                    Id = queue.Id.ToString(),
                    QueueNumber = queue.QueueNumber,
                    Status = "WAITING",
                    CalledAt = null
                }
            });
        }
    }
}