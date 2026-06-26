namespace ECS.Application.Services.DoctorScheduleManagementServices.DoctorDashboardServices
{
    /// <summary>
    /// Response model containing doctor dashboard statistics and trends.
    /// </summary>
    public class DoctorDashboardResponse
    {
        public TodayScheduleSummary TodaySchedule { get; set; } = new();
        public AppointmentStatusSummary TodayAppointments { get; set; } = new();
        public AppointmentStatusSummary PeriodAppointments { get; set; } = new();
        public int TotalPatients { get; set; }
        public List<DailyTrendItem> Trend { get; set; } = [];
    }

    /// <summary>
    /// Summary of today's schedule and time slot availability.
    /// </summary>
    public class TodayScheduleSummary
    {
        public int TotalShifts { get; set; }
        public int TotalSlots { get; set; }
        public int BookedSlots { get; set; }
        public int AvailableSlots { get; set; }
        public int BlockedSlots { get; set; }
    }

    /// <summary>
    /// Appointment statistics grouped by appointment status.
    /// </summary>
    public class AppointmentStatusSummary
    {
        public int Total { get; set; }
        public int ActiveTotal { get; set; }
        public int Pending { get; set; }
        public int DepositPaid { get; set; }
        public int Booked { get; set; }
        public int Arrived { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }
        public int NoShow { get; set; }
    }

    /// <summary>
    /// Daily appointment statistics used for dashboard trend charts.
    /// </summary>
    public class DailyTrendItem
    {
        public DateOnly Date { get; set; }
        public int CompletedCount { get; set; }
        public int TotalCount { get; set; }
    }
}
