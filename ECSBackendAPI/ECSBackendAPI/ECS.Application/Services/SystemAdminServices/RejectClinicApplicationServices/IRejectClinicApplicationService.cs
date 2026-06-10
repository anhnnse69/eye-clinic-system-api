using ECS.Application.Common.Response;

namespace ECS.Application.Services.SystemAdminServices.RejectClinicApplicationServices
{
    public interface IRejectClinicApplicationService
    {
        Task<ApiResponse<bool>> Process(Guid id, RejectClinicApplicationRequest request, Guid adminId);
    }
}
