using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECS.API.BackgroundJobs
{
    /// <summary>
    /// Managed long-running background service that orchestrates automated state transitions for unfulfilled calendar bookings.
    /// Operates as an in-process daemon executing periodic chronological database sweeps based on designated cutoff windows.
    /// </summary>
    public class DailyAppointmentNoShowScheduler : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyAppointmentNoShowScheduler> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="DailyAppointmentNoShowScheduler"/> with core container infrastructure dependencies.
        /// </summary>
        /// <param name="serviceProvider">The root service container provider utilized to generate isolated dependency scopes dynamically during state reconciliation cycles.</param>
        /// <param name="logger">The diagnostic telemetry provider driving targeted operational state logs.</param>
        public DailyAppointmentNoShowScheduler(IServiceProvider serviceProvider, ILogger<DailyAppointmentNoShowScheduler> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Coordinates the operational lifetime loop of the underlying background worker thread.
        /// Calculates chronological delta offsets to delay execution thread blocks until designated cutoff target boundaries are breached.
        /// </summary>
        /// <param name="stoppingToken">The signal monitoring artifact monitoring application shutdown demands to trigger graceful worker thread exit sequences.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Daily Appointment No-Show Background Job da duoc khoi tao.");
            while (!stoppingToken.IsCancellationRequested)
            {
                var tzVn = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzVn);
                // Configure chronological sweep target coordinates mapped to the designated 23:50 localized execution window.
                var nextRunTime = nowVn.Date.AddHours(23).AddMinutes(50);
                // Evaluate timeline boundary overflow parameters; if the current localized timestamp supersedes the current day target coordinate, shift execution delta forward to the next diurnal sequence node.
                if (nowVn > nextRunTime)
                {
                    nextRunTime = nextRunTime.AddDays(1);
                }
                var delayTimeSpan = nextRunTime - nowVn;
                _logger.LogInformation("He thong tu dong quet No-Show se khoi chay sau: {Delay}", delayTimeSpan);
                // Yield asynchronous thread control safely, suspending executing stack states until the designated chronological schedule target node triggers.
                await Task.Delay(delayTimeSpan, stoppingToken);
                if (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await ExecuteAutomaticNoShowSweep();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Co loi xay ra trong qua trinh tu dong quet xu ly lich hen No-Show.");
                    }
                }
            }
        }

        /// <summary>
        /// Encapsulates isolated transaction unit boundaries to fetch, mutate, and reconcile stale calendar scheduling allocations.
        /// </summary>
        private async Task ExecuteAutomaticNoShowSweep()
        {
            _logger.LogInformation("Dang thuc hien quet tu dong lich hen qua han cuoi ngay...");
            // Instantiates an ephemeral operational dependency injection scope to resolve and recycle target DbContext nodes cleanly, mitigating long-running tracking leaks.
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tzVn = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var todayVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzVn).Date;
            // Query pipeline extracting all unfulfilled day allocations bounded within today's date limits retaining premature processing statuses.
            var expiredAppointments = await dbContext.Set<Appointment>()
                .Where(ap => ap.AppointmentDate.Date == todayVn &&
                             (ap.Status == AppointmentStatus.CONFIRMED || ap.Status == AppointmentStatus.BOOKED))
                .ToListAsync();
            if (expiredAppointments.Any())
            {
                foreach (var appointment in expiredAppointments)
                {
                    appointment.Status = AppointmentStatus.NOSHOW;
                    appointment.NoteReason = "Bệnh nhân không đến khám";
                    appointment.UpdatedAt = DateTime.UtcNow;
                }
                int affectedRows = await dbContext.SaveChangesAsync();
                _logger.LogInformation("Da tu dong chuyen doi thanh cong {Count} lich hen sang trang thai NOSHOW.", affectedRows);
            }
            else
            {
                _logger.LogInformation("Khong co lich hen nao bi lo hen trong ngay hom nay.");
            }
        }
    }
}