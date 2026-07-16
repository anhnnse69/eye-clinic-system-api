using ECS.Application.Common.Response;

namespace ECS.Application.Services.ParaclinicalServices.UpdateLabResultServices
{
    public interface IUpdateLabResultService
    {
        Task<ApiResponse<UpdateLabResultResponse>> Process(UpdateLabResultRequest request);
    }
}