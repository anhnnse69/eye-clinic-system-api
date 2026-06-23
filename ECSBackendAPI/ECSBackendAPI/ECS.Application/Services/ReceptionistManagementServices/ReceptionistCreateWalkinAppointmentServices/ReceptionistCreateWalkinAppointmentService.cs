using ECS.Application.Common.Response;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistCreateWalkinAppointmentServices
{
    /// <summary>
    /// Handles the comprehensive business transactions, queue number counters, and database mapping for on-site walk-in appointments.
    /// </summary>
    public class ReceptionistCreateWalkinAppointmentService : IReceptionistCreateWalkinAppointmentService
    {
        private readonly IRepositoryBaseAsync<Appointment, Guid, AppDbContext> _appointmentRepo;
        private readonly IRepositoryBaseAsync<Queue, Guid, AppDbContext> _queueRepo;
        private readonly IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> _slotRepo;
        private readonly IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext> _patientRepo;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistCreateWalkinAppointmentService"/> utilizing multi-entity transaction repositories.
        /// </summary>
        /// <param name="appointmentRepo">The database transaction repository targeting active patient appointments.</param>
        /// <param name="queueRepo">The transactional operational tracking table repository for clinic queues.</param>
        /// <param name="slotRepo">The reading/mutation boundary database data structure repository for schedule slots.</param>
        /// <param name="patientRepo">The master patient identification context verification repository.</param>
        public ReceptionistCreateWalkinAppointmentService(
            IRepositoryBaseAsync<Appointment, Guid, AppDbContext> appointmentRepo,
            IRepositoryBaseAsync<Queue, Guid, AppDbContext> queueRepo,
            IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> slotRepo,
            IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext> patientRepo)
        {
            _appointmentRepo = appointmentRepo;
            _queueRepo = queueRepo;
            _slotRepo = slotRepo;
            _patientRepo = patientRepo;
        }

        /// <summary>
        /// Orchestrates the root transaction flow execution pipeline for the walk-in lifecycle framework.
        /// </summary>
        /// <param name="request">The raw customer parameters mapped from API endpoint boundaries.</param>
        /// <returns>The combined transaction data results wrapped inside a standard core API frame response.</returns>
        public async Task<ApiResponse<ReceptionistCreateWalkinAppointmentResponse>> Process(ReceptionistCreateWalkinAppointmentRequest request)
        {
            return await ExecuteWalkInTransactionPipeline(request);
        }

        /// <summary>
        /// Executes the concrete atomic unit-of-work transaction tree covering entity mutations across distinct aggregates.
        /// </summary>
        /// <param name="request">The operational properties defining target clinic requirements.</param>
        /// <returns>The structured transactional status payload context envelope.</returns>
        private async Task<ApiResponse<ReceptionistCreateWalkinAppointmentResponse>> ExecuteWalkInTransactionPipeline(ReceptionistCreateWalkinAppointmentRequest request)
        {
            using var transaction = await _appointmentRepo.BeginTransactionAsync();
            try
            {
                var patient = await _patientRepo.GetByIdAsync(request.PatientProfileId);
                if (patient == null)
                    return ApiResponse<ReceptionistCreateWalkinAppointmentResponse>.Fail("APP_MESSAGE_PATIENT_NOT_FOUND");
                DateTime todayVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Date;
                var slotSample = await _slotRepo.FindByCondition(
                    s => s.ScheduleId == request.DoctorId || s.Schedule.DoctorId == request.DoctorId,
                    trackChanges: false
                ).Include(s => s.Schedule)
                 .Include(s => s.Schedule.Room)
                 .Include(s => s.Schedule.Doctor)
                 .OrderByDescending(s => s.StartTime)
                 .FirstOrDefaultAsync();
                if (slotSample == null)
                {
                    slotSample = await _slotRepo.FindByCondition(x => true, trackChanges: false)
                     .Include(s => s.Schedule)
                     .Include(s => s.Schedule.Room)
                     .Include(s => s.Schedule.Doctor)
                     .OrderByDescending(s => s.StartTime)
                     .FirstOrDefaultAsync();
                }
                if (slotSample == null)
                    return ApiResponse<ReceptionistCreateWalkinAppointmentResponse>.Fail("DATABASE_EMPTY_NO_SLOTS_EXIST");
                var schedule = slotSample.Schedule;
                Guid realDoctorId = schedule.DoctorId;
                Guid targetRoomId = schedule?.Room?.Id ?? Guid.Empty;
                Guid clinicId = schedule?.Doctor?.ClinicId ?? slotSample.Schedule.Doctor.ClinicId;
                if (targetRoomId == Guid.Empty)
                    return ApiResponse<ReceptionistCreateWalkinAppointmentResponse>.Fail("DOCTOR_ROOM_NOT_CONFIGURED");
                DateTime startOfDayUtc = TimeZoneInfo.ConvertTimeToUtc(todayVn, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
                DateTime endOfDayUtc = startOfDayUtc.AddDays(1);
                int maxQueueNumber = await _queueRepo.FindByCondition(
                    q => q.RoomId == targetRoomId && q.CreatedAt >= startOfDayUtc && q.CreatedAt < endOfDayUtc,
                    trackChanges: false
                ).Select(q => (int?)q.QueueNumber).MaxAsync() ?? 0;
                int nextNumber = maxQueueNumber + 1;
                var appointment = new Appointment
                {
                    Id = Guid.NewGuid(),
                    PatientId = request.PatientProfileId,
                    DoctorId = realDoctorId,
                    SlotId = slotSample.Id,
                    ServiceId = request.ServiceId,
                    AppointmentDate = todayVn,
                    Symptoms = request.Symptoms ?? "Khám vãng lai tại quầy (Đăng ký trực tiếp)",
                    Status = AppointmentStatus.ARRIVED,
                    DepositAmount = 0,
                    DepositPaid = true,
                    BookingSource = "WALKIN",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _appointmentRepo.CreateAsync(appointment);
                var newQueue = new Queue
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointment.Id,
                    ClinicId = clinicId,
                    RoomId = targetRoomId,
                    QueueNumber = nextNumber,
                    Status = QueueStatus.WAITING,
                    CreatedAt = DateTime.UtcNow
                };
                await _queueRepo.CreateAsync(newQueue);
                await _appointmentRepo.EndTransactionAsync();
                return AssembleSuccessWalkInResponse(appointment, newQueue);
            }
            catch (Exception)
            {
                await _appointmentRepo.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Groups and formats the newly instantiated entity models into a successful outcome response payload wrapper.
        /// </summary>
        /// <param name="appointment">The tracking source appointment record data.</param>
        /// <param name="queue">The operational tracking line queue object segment context.</param>
        /// <returns>A formatted standard api response structured package wrapper.</returns>
        private ApiResponse<ReceptionistCreateWalkinAppointmentResponse> AssembleSuccessWalkInResponse(Appointment appointment, Queue queue)
        {
            return ApiResponse<ReceptionistCreateWalkinAppointmentResponse>.Success("APP_MESSAGE_2000", new ReceptionistCreateWalkinAppointmentResponse
            {
                AppointmentId = appointment.Id.ToString(),
                Status = appointment.Status.ToString().ToUpper(),
                WalkInQueue = new QueueInlineRowDto
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