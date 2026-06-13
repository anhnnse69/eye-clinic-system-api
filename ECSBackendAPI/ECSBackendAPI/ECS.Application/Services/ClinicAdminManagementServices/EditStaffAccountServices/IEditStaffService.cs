using System.Threading.Tasks;
using ECS.Application.Common.Response;

namespace ECS.Application.Services.ClinicAdminManagementServices.EditStaffAccountServices
{
    /// <summary>
    /// Service contract defining persistent state update operations for operational clinic staff identity accounts.
    /// </summary>
    public interface IEditStaffService
    {
        /// <summary>
        /// Executes the application workflow engine process to modify data mappings for existing staff member accounts.
        /// </summary>
        /// <param name="request">The specialized edit parameter model structure payload details.</param>
        /// <returns>A unified data wrapper structural tracking container outcome report token.</returns>
        Task<ApiResponse<EditStaffResponse>> Process(EditStaffRequest request);
    }
}