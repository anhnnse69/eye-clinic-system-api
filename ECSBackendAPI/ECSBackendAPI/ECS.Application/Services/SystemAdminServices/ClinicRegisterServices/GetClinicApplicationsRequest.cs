namespace ECS.Application.Services.SystemAdminServices.ClinicRegisterServices
{
    public class GetClinicApplicationsRequest
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; } // PENDING, APPROVED, REJECTED
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
