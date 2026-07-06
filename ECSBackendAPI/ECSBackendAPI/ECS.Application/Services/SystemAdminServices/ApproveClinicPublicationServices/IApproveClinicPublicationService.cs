using ECS.Application.Common.Response;

namespace ECS.Application.Services.SystemAdminServices.ApproveClinicPublicationServices
{
    /// <summary>
    /// Contract defining execution rules for approving a clinic's publication request.
    /// </summary>
    public interface IApproveClinicPublicationService
    {
        /// <summary>
        /// Dispatches the validation and state machine mutation for clinic publication.
        /// </summary>
        /// <param name="clinicId">The unique structural database identifier of the clinic.</param>
        /// <returns>A standard api structural contract signaling business outcome success flags.</returns>
        Task<ApiResponse<bool>> Process(Guid id, Guid adminId);
    }
}
