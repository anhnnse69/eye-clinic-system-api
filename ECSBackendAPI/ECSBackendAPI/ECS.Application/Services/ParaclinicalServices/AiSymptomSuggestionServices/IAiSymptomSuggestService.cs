using ECS.Application.Common.Response;

namespace ECS.Application.Services.ParaclinicalServices.AiSymptomSuggestionServices
{
    public interface IAiSymptomSuggestService
    {
        Task<ApiResponse<AiSymptomSuggestResponse>> Process(AiSymptomSuggestRequest request);
    }
}
