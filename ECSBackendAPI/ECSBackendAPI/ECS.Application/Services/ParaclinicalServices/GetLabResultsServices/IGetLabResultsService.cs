using ECS.Application.Common.Response;

namespace ECS.Application.Services.ParaclinicalServices.GetLabResultsServices
{
    public interface IGetLabResultsService
    {
        Task<ApiResponse<GetLabResultsResponse>> Process(GetLabResultsRequest request);
    }
}