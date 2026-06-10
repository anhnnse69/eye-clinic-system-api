using ECS.Application.Common.Response;

namespace ECS.Application.Services.SystemAdminServices.ReviewClinicRegisterServices
{
    public interface IGetClinicApplicationDetailService
    {
        Task<ApiResponse<GetClinicApplicationDetailResponse>> Process(Guid id);
    }
}
