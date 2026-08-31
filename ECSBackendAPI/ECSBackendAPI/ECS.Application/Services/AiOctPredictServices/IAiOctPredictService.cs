using ECS.Application.Common.Response;
using ECS.Infrastructure.Ai;

namespace ECS.Application.Services.AiOctPredictServices;

public interface IAiOctPredictService
{
    Task<ApiResponse<AiOctPredictResponse>> ProcessAsync(AiOctPredictRequest request, CancellationToken ct = default);
}
