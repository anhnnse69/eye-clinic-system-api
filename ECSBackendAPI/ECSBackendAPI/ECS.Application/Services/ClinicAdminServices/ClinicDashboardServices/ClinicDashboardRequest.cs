// ClinicDashboardRequest.cs
namespace ECS.Application.Services.DashboardServices.ClinicDashboardServices
{
    /// <summary>
    /// Request model for retrieving Clinic Admin dashboard statistics.
    /// ClinicId is required. DateFrom and DateTo are optional filters.
    /// </summary>
    public class ClinicDashboardRequest
    {
        public Guid ClinicId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}