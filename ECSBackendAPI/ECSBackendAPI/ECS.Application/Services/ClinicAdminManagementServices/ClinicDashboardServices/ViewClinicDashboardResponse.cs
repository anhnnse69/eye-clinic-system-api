namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicDashboardServices
{
    /// <summary>
    /// Response object containing dashboard statistics and chart data.
    /// </summary>
    public class ViewClinicDashboardResponse
    {
        /// <summary>
        /// Total appointments scheduled for today.
        /// </summary>
        public int TotalAppointments { get; set; }

        /// <summary>
        /// Number of completed appointments.
        /// </summary>
        public int CompletedAppointments { get; set; }

        /// <summary>
        /// Number of cancelled appointments.
        /// </summary>
        public int CancelledAppointments { get; set; }

        /// <summary>
        /// Total revenue generated from paid deposits.
        /// </summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// Total number of active staff members.
        /// </summary>
        public int TotalStaffs { get; set; }

        /// <summary>
        /// Total number of active services.
        /// </summary>
        public int TotalServices { get; set; }

        /// <summary>
        /// Total number of active facility rooms.
        /// </summary>
        public int TotalRooms { get; set; }

        /// <summary>
        /// Total number of active medicines.
        /// </summary>
        public int TotalMedicines { get; set; }

        /// <summary>
        /// Weekly revenue and appointment statistics.
        /// </summary>
        public List<DashboardChartItem> WeeklyStatistics { get; set; } = [];
    }
}