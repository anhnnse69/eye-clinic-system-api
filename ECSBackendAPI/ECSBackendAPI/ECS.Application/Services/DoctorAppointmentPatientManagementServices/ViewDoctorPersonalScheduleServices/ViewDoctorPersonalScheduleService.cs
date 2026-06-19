using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewDoctorPersonalScheduleServices
{
    public class ViewDoctorPersonalScheduleService : IViewDoctorPersonalScheduleService
    {
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepo;
        private readonly IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext> _scheduleRepo;

        public ViewDoctorPersonalScheduleService(
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepo,
            IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext> scheduleRepo)
        {
            _doctorRepo = doctorRepo;
            _scheduleRepo = scheduleRepo;
        }

        public async Task<ApiResponse<ViewDoctorPersonalScheduleResponse>> Process(
            Guid userId,
            ViewDoctorPersonalScheduleRequest request)
        {
            var doctor = await ResolveActiveDoctorAsync(userId);
            var schedules = await FetchSchedulesAsync(
                doctor.Id,
                request.WorkDate,
                request.ShiftType);
            var shifts = MapToShiftItems(schedules);
            var response = BuildResponse(request.WorkDate, shifts);
            return CreateSuccessResponse(response);
        }

        // ── Private helpers ───────────────────────────────────────────────

        /// <summary>
        /// Resolves the active doctor profile for the given user.
        /// Throws <see cref="KeyNotFoundException"/> when not found.
        /// </summary>
        private async Task<DoctorProfile> ResolveActiveDoctorAsync(Guid userId)
        {
            return await _doctorRepo
                .FindByCondition(d => d.UserId == userId && d.IsActive)
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException(
                    GeneralCode.APP_MESSAGE_4008.ToString());
        }

        /// <summary>
        /// Fetches schedules for the doctor on the given date,
        /// optionally filtered by shift type.
        /// </summary>
        private async Task<List<DoctorSchedule>> FetchSchedulesAsync(
            Guid doctorId,
            DateOnly workDate,
            ShiftType? shiftType)
        {
            var targetDate = workDate.ToDateTime(TimeOnly.MinValue);

            var query = _scheduleRepo
                .FindByCondition(s =>
                    s.DoctorId == doctorId &&
                    s.WorkDate.Date == targetDate.Date)
                .Include(s => s.Room)
                .Include(s => s.TimeSlots)
                    .ThenInclude(slot => slot.Appointments)
                        .ThenInclude(a => a.Patient)
                .AsQueryable();
            if (shiftType.HasValue)
                query = query.Where(s => s.ShiftType == shiftType.Value);
            return await query
                .OrderBy(s => s.ShiftType)
                .ToListAsync();
        }

        /// <summary>
        /// Maps a list of <see cref="DoctorSchedule"/> entities
        /// to <see cref="ScheduleShiftItem"/> DTOs.
        /// </summary>
        private static List<ScheduleShiftItem> MapToShiftItems(
            List<DoctorSchedule> schedules)
        {
            return schedules
                .Select(MapToShiftItem)
                .ToList();
        }

        /// <summary>
        /// Maps a single <see cref="DoctorSchedule"/> to a
        /// <see cref="ScheduleShiftItem"/> DTO.
        /// </summary>
        private static ScheduleShiftItem MapToShiftItem(DoctorSchedule schedule)
        {
            return new ScheduleShiftItem
            {
                ScheduleId = schedule.Id,
                ShiftType = schedule.ShiftType,
                Note = schedule.Note,
                RoomId = schedule.Room?.Id,
                RoomName = schedule.Room?.RoomName,
                Slots = MapToSlotItems(schedule.TimeSlots),
            };
        }

        /// <summary>
        /// Maps a collection of <see cref="TimeSlot"/> entities
        /// to <see cref="ScheduleSlotItem"/> DTOs.
        /// </summary>
        private static List<ScheduleSlotItem> MapToSlotItems(
            ICollection<TimeSlot>? timeSlots)
        {
            return (timeSlots ?? [])
                .OrderBy(slot => slot.StartTime)
                .Select(MapToSlotItem)
                .ToList();
        }

        /// <summary>
        /// Maps a single <see cref="TimeSlot"/> to a
        /// <see cref="ScheduleSlotItem"/> DTO.
        /// </summary>
        private static ScheduleSlotItem MapToSlotItem(TimeSlot slot)
        {
            return new ScheduleSlotItem
            {
                SlotId = slot.Id,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                MaxPatients = slot.MaxPatients,
                CurrentPatients = slot.CurrentPatients,
                Status = slot.Status,
                Appointments = MapToAppointmentItems(slot.Appointments),
            };
        }

        /// <summary>
        /// Maps non-cancelled appointments to
        /// <see cref="SlotAppointmentItem"/> DTOs.
        /// </summary>
        private static List<SlotAppointmentItem> MapToAppointmentItems(
            ICollection<Appointment>? appointments)
        {
            return (appointments ?? [])
                .Where(a => a.Status != AppointmentStatus.CANCELLED)
                .Select(a => new SlotAppointmentItem
                {
                    AppointmentId = a.Id,
                    PatientId = a.PatientId,
                    PatientName = a.Patient.FullName,
                    PatientPhone = a.Patient.PhoneNumber,
                    Status = a.Status.ToString(),
                    Symptoms = a.Symptoms,
                })
                .ToList();
        }

        /// <summary>
        /// Builds the response object.
        /// </summary>
        private static ViewDoctorPersonalScheduleResponse BuildResponse(
            DateOnly workDate,
            List<ScheduleShiftItem> shifts)
        {
            return new ViewDoctorPersonalScheduleResponse
            {
                WorkDate = workDate,
                Shifts = shifts,
            };
        }

        /// <summary>
        /// Wraps the response in a success API envelope.
        /// </summary>
        private static ApiResponse<ViewDoctorPersonalScheduleResponse> CreateSuccessResponse(
            ViewDoctorPersonalScheduleResponse response)
        {
            return ApiResponse<ViewDoctorPersonalScheduleResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }
    }
}