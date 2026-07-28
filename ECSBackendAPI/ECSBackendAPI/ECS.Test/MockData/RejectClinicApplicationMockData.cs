using ECS.Application.Services.SystemAdminServices.RejectClinicApplicationServices;
using ECS.Domain.Entities.Clinics;

namespace ECS.Test.MockData
{
    public static class RejectClinicApplicationMockData
    {
        public static readonly Guid AdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ApplicationId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public static RejectClinicApplicationRequest GetValidRequest() => new()
        {
            ReviewNote = "Thông tin đăng ký không đáp ứng tiêu chuẩn hệ thống."
        };

        public static ClinicRegistrationRequest GetPendingApplication(Guid? id = null) => new()
        {
            Id = id ?? ApplicationId,
            ClinicName = "Phòng khám Đa khoa ABC",
            Status = "PENDING",
            ReviewNote = null,
            ReviewedBy = null,
            ReviewedAt = null
        };

        public static ClinicRegistrationRequest GetNonPendingApplication(Guid? id = null, string status = "APPROVED") => new()
        {
            Id = id ?? ApplicationId,
            ClinicName = "Phòng khám Đa khoa ABC",
            Status = status
        };
    }
}