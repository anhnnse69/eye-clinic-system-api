using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.DoctorDashboardServices
{
    /// <summary>
    /// Aggregates dashboard metrics for a doctor: today's schedule summary,
    /// today's appointment statuses, period (week/month) appointment trend,
    /// total distinct patients, and today's upcoming appointments.
    /// </summary>
    public class DoctorDashboardService : IDoctorDashboardService
    {
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>
            _doctorRepo;
        private readonly IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext>
            _scheduleRepo;
        private readonly IRepositoryQueryBase<Appointment, Guid, AppDbContext>
            _appointmentRepo;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoctorDashboardService"/> class.
        /// </summary>
        /// <param name="doctorRepo">Doctor profile repository.</param>
        /// <param name="scheduleRepo">Doctor schedule repository.</param>
        /// <param name="appointmentRepo">Appointment repository.</param>
        public DoctorDashboardService(
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepo,
            IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext> scheduleRepo,
            IRepositoryQueryBase<Appointment, Guid, AppDbContext> appointmentRepo)
        {
            _doctorRepo = doctorRepo;
            _scheduleRepo = scheduleRepo;
            _appointmentRepo = appointmentRepo;
        }

        /// <summary>
        /// Builds the doctor's dashboard for today plus a custom period.
        /// </summary>
        /// <param name="userId">
        /// Identifier of the user account linked to the doctor profile.
        /// </param>
        /// <param name="request">Optional start/end date for the trend period.</param>
        /// <returns>A successful response containing the dashboard data.</returns>
        public async Task<ApiResponse<DoctorDashboardResponse>> Process(
            Guid userId,
            DoctorDashboardRequest request)
        {
            var doctorProfile = await ResolveActiveDoctorProfileAsync(userId);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var (periodStart, periodEnd) = NormalizePeriod(request, today);
            var todaySchedule = await FetchTodayScheduleSummaryAsync(
                doctorProfile.Id, today);
            var todayAppointments = await FetchAppointmentSummaryAsync(
                doctorProfile.Id, today, today);
            var periodAppointments = await FetchAppointmentSummaryAsync(
                doctorProfile.Id, periodStart, periodEnd);
            var totalPatients = await CountDistinctPatientsAsync(doctorProfile.Id);
            var trend = await BuildDailyTrendAsync(
                doctorProfile.Id, periodStart, periodEnd);
            var response = BuildResponse(
                todaySchedule,
                todayAppointments,
                periodAppointments,
                totalPatients,
                trend);
            return CreateSuccessResponse(response);
        }

        /// <summary>
        /// Resolves the active doctor profile for the specified user.
        /// Throws when not found.
        /// </summary>
        private async Task<DoctorProfile> ResolveActiveDoctorProfileAsync(
            Guid userId)
        {
            var doctorProfile = await _doctorRepo
                .FindByCondition(d =>
                    d.UserId == userId &&
                    d.IsActive)
                .FirstOrDefaultAsync();

            if (doctorProfile is null)
                throw new KeyNotFoundException(
                    GeneralCode.APP_MESSAGE_4008.ToString());

            return doctorProfile;
        }

        /// <summary>
        /// Normalizes the requested period, defaulting to the last 7 days
        /// ending today when not provided.
        /// </summary>
        private static (DateOnly Start, DateOnly End) NormalizePeriod(
            DoctorDashboardRequest request,
            DateOnly today)
        {
            var end = request.EndDate ?? today;
            var start = request.StartDate ?? end.AddDays(-6);
            if (start > end)
                (start, end) = (end, start);
            return (start, end);
        }

        /// <summary>
        /// Vietnam standard time zone used for date and time calculations.
        /// </summary>
        private static readonly TimeZoneInfo VietnamTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        /// <summary>
        /// Fetches today's schedule shift/slot counts for the doctor.
        /// </summary>
        /// <summary>
        /// Fetches today's schedule shift/slot counts for the doctor.
        /// Slots that are still AVAILABLE but already past their start time
        /// by more than 30 minutes are treated as BLOCKED (expired), matching
        /// the same rule applied on the personal schedule view.
        /// </summary>
        private async Task<TodayScheduleSummary> FetchTodayScheduleSummaryAsync(
            Guid doctorId,
            DateOnly today)
        {
            var todayDateTime = today.ToDateTime(TimeOnly.MinValue);
            var schedules = await _scheduleRepo
                .FindByCondition(s =>
                    s.DoctorId == doctorId &&
                    s.WorkDate.Date == todayDateTime.Date &&
                    !s.IsDeleted)
                .Include(s => s.TimeSlots)
                .ToListAsync();
            var allSlots = schedules
                .SelectMany(s => s.TimeSlots ?? [])
                .ToList();
            var nowVietnam = TimeZoneInfo.ConvertTime(DateTime.UtcNow, VietnamTimeZone);
            var effectiveStatuses = allSlots
                .Select(slot => GetEffectiveSlotStatus(slot, nowVietnam))
                .ToList();
            return new TodayScheduleSummary
            {
                TotalShifts = schedules.Count,
                TotalSlots = allSlots.Count,
                BookedSlots = effectiveStatuses.Count(s => s == SlotStatus.BOOKED),
                AvailableSlots = effectiveStatuses.Count(s => s == SlotStatus.AVAILABLE),
                BlockedSlots = effectiveStatuses.Count(s => s == SlotStatus.BLOCKED),
            };
        }

        /// <summary>
        /// Determines the effective status of a slot: an AVAILABLE slot whose
        /// start time has already passed by 30+ minutes is treated as BLOCKED
        /// (expired and no longer bookable), even though its stored status is
        /// still AVAILABLE.
        /// </summary>
        private static SlotStatus GetEffectiveSlotStatus(TimeSlot slot, DateTime now)
        {
            if (slot.Status != SlotStatus.AVAILABLE)
                return slot.Status;
            var diffInMinutes = (now - slot.StartTime).TotalMinutes;
            var isExpired = diffInMinutes >= 30;
            return isExpired ? SlotStatus.BLOCKED : SlotStatus.AVAILABLE;
        }

        /// <summary>
        /// Fetches appointment status counts for the doctor within the
        /// given inclusive date range.
        /// </summary>
        private async Task<AppointmentStatusSummary> FetchAppointmentSummaryAsync(
            Guid doctorId,
            DateOnly start,
            DateOnly end)
        {
            var startDateTime = start.ToDateTime(TimeOnly.MinValue);
            var endDateTime = end.ToDateTime(TimeOnly.MaxValue);
            var appointments = await _appointmentRepo
                .FindByCondition(a =>
                    a.DoctorId == doctorId &&
                    a.AppointmentDate >= startDateTime &&
                    a.AppointmentDate <= endDateTime)
                .Select(a => a.Status)
                .ToListAsync();
            var cancelledCount = appointments.Count(s => s == AppointmentStatus.CANCELLED);
            var noShowCount = appointments.Count(s => s == AppointmentStatus.NOSHOW);
            return new AppointmentStatusSummary
            {
                Total = appointments.Count,
                ActiveTotal = appointments.Count - cancelledCount - noShowCount, // ← thêm
                Pending = appointments.Count(s => s == AppointmentStatus.PENDING),
                DepositPaid = appointments.Count(s => s == AppointmentStatus.DEPOSIT_PAID),
                Booked = appointments.Count(s => s == AppointmentStatus.BOOKED),
                Arrived = appointments.Count(s => s == AppointmentStatus.ARRIVED),
                InProgress = appointments.Count(s => s == AppointmentStatus.IN_PROGRESS),
                Completed = appointments.Count(s => s == AppointmentStatus.COMPLETED),
                Cancelled = cancelledCount,
                NoShow = noShowCount,
            };
        }

        /// <summary>
        /// Counts the number of distinct patients the doctor has ever
        /// had an appointment with.
        /// </summary>
        private async Task<int> CountDistinctPatientsAsync(Guid doctorId)
        {
            return await _appointmentRepo
                .FindByCondition(a => 
                    a.DoctorId == doctorId &&
                    a.Status == AppointmentStatus.COMPLETED)
                .Select(a => a.PatientId)
                .Distinct()
                .CountAsync();
        }

        /// <summary>
        /// Builds a day-by-day trend of total and completed appointments
        /// across the given inclusive date range.
        /// </summary>
        private async Task<List<DailyTrendItem>> BuildDailyTrendAsync(
            Guid doctorId,
            DateOnly start,
            DateOnly end)
        {
            var startDateTime = start.ToDateTime(TimeOnly.MinValue);
            var endDateTime = end.ToDateTime(TimeOnly.MaxValue);
            var appointments = await _appointmentRepo
                .FindByCondition(a =>
                    a.DoctorId == doctorId &&
                    a.AppointmentDate >= startDateTime &&
                    a.AppointmentDate <= endDateTime)
                .Select(a => new { a.AppointmentDate, a.Status })
                .ToListAsync();
            var grouped = appointments
                .GroupBy(a => DateOnly.FromDateTime(a.AppointmentDate))
                .ToDictionary(g => g.Key, g => g.ToList());
            var trend = new List<DailyTrendItem>();
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                var dayAppointments = grouped.TryGetValue(date, out var list)
                    ? list
                    : [];
                trend.Add(new DailyTrendItem
                {
                    Date = date,
                    TotalCount = dayAppointments.Count,
                    CompletedCount = dayAppointments
                        .Count(a => a.Status == AppointmentStatus.COMPLETED),
                });
            }

            return trend;
        }

        /// <summary>
        /// Builds the aggregated dashboard response.
        /// </summary>
        private static DoctorDashboardResponse BuildResponse(
            TodayScheduleSummary todaySchedule,
            AppointmentStatusSummary todayAppointments,
            AppointmentStatusSummary periodAppointments,
            int totalPatients,
            List<DailyTrendItem> trend)
        {
            return new DoctorDashboardResponse
            {
                TodaySchedule = todaySchedule,
                TodayAppointments = todayAppointments,
                PeriodAppointments = periodAppointments,
                TotalPatients = totalPatients,
                Trend = trend
            };
        }

        /// <summary>
        /// Creates a successful API response.
        /// </summary>
        private static ApiResponse<DoctorDashboardResponse> CreateSuccessResponse(
            DoctorDashboardResponse response)
        {
            return ApiResponse<DoctorDashboardResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }
    }
}
