using ECS.Application.Common.Response;

namespace ECS.Application.Services.SystemAdminServices.ClinicRegisterServices
{
    public interface IGetClinicApplicationService
    {
        Task<ApiResponse<List<GetClinicApplicationResponse>>> Process(GetClinicApplicationsRequest request);
    }
}
