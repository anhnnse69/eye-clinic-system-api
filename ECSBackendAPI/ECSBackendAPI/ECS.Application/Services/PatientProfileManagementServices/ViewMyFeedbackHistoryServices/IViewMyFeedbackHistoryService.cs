using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientProfileManagementServices.ViewMyFeedbackHistoryServices
{
    /// <summary>
    /// Defines workflow contract for viewing feedback history.
    /// </summary>
    public interface IViewMyFeedbackHistoryService
    {
        Task<ApiResponse<List<ViewMyFeedbackHistoryResponse>>> Process(
            ViewMyFeedbackHistoryRequest request);
    }
}
