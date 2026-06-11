namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicDashboardServices
{
    /// <summary>
    /// Represents daily chart statistics displayed on the dashboard.
    /// </summary>
    public class DashboardChartItem
    {
        /// <summary>
        /// The reporting date.
        /// </summary>
        public string Date { get; set; } = string.Empty;

        /// <summary>
        /// Revenue generated on the specified date.
        /// </summary>
        public decimal Revenue { get; set; }

        /// <summary>
        /// Number of appointments on the specified date.
        /// </summary>
        public int Appointments { get; set; }
    }
}
