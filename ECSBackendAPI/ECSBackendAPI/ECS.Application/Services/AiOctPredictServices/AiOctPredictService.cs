using ECS.Application.Common.Response;
using ECS.Domain.Enums;
using ECS.Infrastructure.Ai;
using Microsoft.Extensions.Logging;

namespace ECS.Application.Services.AiOctPredictServices;

public class AiOctPredictService : IAiOctPredictService
{
    private readonly IAiServiceClient _aiClient;
    private readonly ILogger<AiOctPredictService> _logger;

    public AiOctPredictService(
        IAiServiceClient aiClient,
        ILogger<AiOctPredictService> logger)
    {
        _aiClient = aiClient;
        _logger = logger;
    }

    public async Task<ApiResponse<AiOctPredictResponse>> ProcessAsync(
        AiOctPredictRequest request,
        CancellationToken ct = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            return ApiResponse<AiOctPredictResponse>.Fail(GeneralCode.APP_MESSAGE_4003.ToString());
        }

        try
        {
            var response = await _aiClient.PredictOctImageAsync(request, ct);
            return ApiResponse<AiOctPredictResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }
        catch (AiServiceException ex)
        {
            _logger.LogError(ex, "AI OCT Prediction Service call failed");
            var errResp = new AiOctPredictResponse
            {
                Status = "FAILED",
                ErrorCode = "AI_SERVICE_ERROR",
                ErrorMessage = ex.Message
            };
            return ApiResponse<AiOctPredictResponse>.Success(
                GeneralCode.APP_MESSAGE_5001.ToString(),
                errResp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in AiOctPredictService");
            return ApiResponse<AiOctPredictResponse>.Fail(GeneralCode.APP_MESSAGE_5001.ToString());
        }
    }
}
