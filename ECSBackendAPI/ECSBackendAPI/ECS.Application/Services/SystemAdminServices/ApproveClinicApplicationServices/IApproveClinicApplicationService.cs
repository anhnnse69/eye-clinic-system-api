using ECS.Application.Common.Response;

namespace ECS.Application.Services.SystemAdminServices.ApproveClinicApplicationServices
{
    public interface IApproveClinicApplicationService
    {
        Task<ApiResponse<bool>> Process(Guid id, Guid adminId);
    }
}
