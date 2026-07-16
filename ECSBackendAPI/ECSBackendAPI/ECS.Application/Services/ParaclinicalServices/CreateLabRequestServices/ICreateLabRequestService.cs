using ECS.Application.Common.Response;

namespace ECS.Application.Services.ParaclinicalServices.CreateLabRequestServices
{
    public interface ICreateLabRequestService
    {
        Task<ApiResponse<CreateLabRequestResponse>> Process(CreateLabRequestRequest request);
    }
}