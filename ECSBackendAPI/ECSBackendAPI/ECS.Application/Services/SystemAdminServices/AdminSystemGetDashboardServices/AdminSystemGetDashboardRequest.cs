namespace ECS.Application.Services.SystemAdminServices.AdminSystemGetDashboardServices
{
    /// <summary>
    /// Request object containing filter parameters for the system admin dashboard query.
    /// </summary>
    public class AdminSystemGetDashboardRequest
    {
        public Guid? ClinicId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
