using ECS.Application.Common.Response;

namespace ECS.Application.Services.ParaclinicalServices.AiSuggestionServices
{
    public interface IAiSuggestService
    {
        Task<ApiResponse<AiSuggestResponse>> Process(AiSuggestRequest request);
    }
}